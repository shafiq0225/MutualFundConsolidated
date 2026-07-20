using Microsoft.EntityFrameworkCore;
using MutualFund.Auth.Domain.Entities;
using MutualFund.Auth.Domain.Enums;
using MutualFund.Auth.Domain.Exceptions;
using MutualFund.Auth.Domain.Interfaces;
using MutualFund.Auth.Infrastructure.Data;

namespace MutualFund.Auth.Infrastructure.Services
{
    public class FamilyService : IFamilyService
    {
        private readonly ApplicationDbContext _context;

        public FamilyService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<FamilyGroup>> GetAllFamilyGroupsAsync() =>
            await _context.FamilyGroups
                .Include(g => g.HeadUser)
                .Include(g => g.Members)
                    .ThenInclude(m => m.User)
                .Where(g => g.IsActive)
                .OrderBy(g => g.GroupName)
                .ToListAsync();

        public async Task<FamilyGroup> GetFamilyGroupAsync(int groupId) =>
            await _context.FamilyGroups
                .Include(g => g.HeadUser)
                .Include(g => g.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.Id == groupId)
                ?? throw new FamilyGroupNotFoundException(groupId);

        public async Task<FamilyGroup> CreateFamilyGroupAsync(
            string groupName,
            string headUserId,
            string adminId)
        {
            // Verify head user exists and is a User role
            var headUser = await _context.Users
                .FindAsync(headUserId)
                ?? throw new UserNotFoundException(headUserId);

            if (headUser.Role != UserRole.User)
                throw new UnauthorizedActionException(
                    "assign a non-User role account as Head of Family");

            // Check head is not already in a family group
            var alreadyInGroup = await _context.FamilyMembers
                .AnyAsync(m => m.UserId == headUserId);
            if (alreadyInGroup)
                throw new UserAlreadyInFamilyException(headUserId);

            // Set UserType to HeadOfFamily
            headUser.UserType = UserType.HeadOfFamily;

            var group = new FamilyGroup
            {
                GroupName = groupName.Trim(),
                HeadUserId = headUserId,
                CreatedByAdminId = adminId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.FamilyGroups.Add(group);
            await _context.SaveChangesAsync();

            return await GetFamilyGroupAsync(group.Id);
        }

        public async Task<FamilyGroup> AddMemberAsync(
            int groupId,
            string userId,
            FamilyRelationshipType relationshipType,
            string? displayLabel,
            string adminId)
        {
            // Self is reserved for the Head's own synthesized entry —
            // never a real, stored FamilyMember row.
            if (relationshipType == FamilyRelationshipType.Self)
                throw new UnauthorizedActionException(
                    "assign RelationshipType.Self to a family member — " +
                    "Self is reserved for the Head of Family");

            var group = await GetFamilyGroupAsync(groupId);

            // Verify user exists and is User role
            var user = await _context.Users.FindAsync(userId)
                ?? throw new UserNotFoundException(userId);

            if (user.Role != UserRole.User)
                throw new UnauthorizedActionException(
                    "add a non-User role account to a family group");

            // Cannot add Head as a member
            if (userId == group.HeadUserId)
                throw new UnauthorizedActionException(
                    "add the Head of Family as a member");

            // Check not already in any family group
            var alreadyInGroup = await _context.FamilyMembers
                .AnyAsync(m => m.UserId == userId);
            if (alreadyInGroup)
                throw new UserAlreadyInFamilyException(userId);

            // Set UserType to FamilyMember
            user.UserType = UserType.FamilyMember;

            _context.FamilyMembers.Add(new FamilyMember
            {
                FamilyGroupId = groupId,
                UserId = userId,
                RelationshipType = relationshipType,
                DisplayLabel = displayLabel?.Trim(),
                AddedByAdminId = adminId,
                AddedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return await GetFamilyGroupAsync(groupId);
        }

        public async Task RemoveMemberAsync(
            int groupId, string userId, string adminId)
        {
            await GetFamilyGroupAsync(groupId);

            var member = await _context.FamilyMembers
                .FirstOrDefaultAsync(m =>
                    m.FamilyGroupId == groupId &&
                    m.UserId == userId)
                ?? throw new UserNotInFamilyException(userId, groupId);

            // Reset UserType
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
                user.UserType = UserType.None;

            _context.FamilyMembers.Remove(member);
            await _context.SaveChangesAsync();
        }
    }
}