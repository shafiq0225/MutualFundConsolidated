"""
SQL Server -> MySQL Migration Script
Source : db59687.public.databaseasp.net (SQL Server)
Target : localhost:3306 MutualFundDbV2_Prod (MySQL)
Run    : python migrate_sqlserver_to_mysql.py
"""

import pymssql
import mysql.connector

MSSQL = dict(server='db59687.public.databaseasp.net', user='db59687',
             password='3Tc=a6#A+mD8', database='db59687', timeout=30)
MYSQL = dict(host='localhost', port=3306, database='MutualFundDbV2_Prod',
             user='root', password='', charset='utf8mb4')

def section(t):
    print(f"\n{'='*58}\n  {t}\n{'='*58}")

def ok(m):   print(f"  [OK]  {m}")
def skip(m): print(f"  [--]  {m}")
def warn(m): print(f"  [!!]  {m}")
def err(m):  print(f"  [ERR] {m}")

def fetchall(cur, sql):
    cur.execute(sql)
    cols = [d[0] for d in cur.description]
    return cols, cur.fetchall()

def s(v):
    if v is None: return None
    if isinstance(v, (bytes, bytearray)): return v.decode('utf-8', errors='replace')
    return v

# ─── Connect ───────────────────────────────────────────────────────────────
section("Connecting to databases")
src = pymssql.connect(**MSSQL)
sc  = src.cursor()
ok("SQL Server connected")

dst = mysql.connector.connect(**MYSQL)
dc  = dst.cursor()
ok("MySQL connected")

dc.execute("SET FOREIGN_KEY_CHECKS=0")
dc.execute("SET sql_mode=''")

# ─── 1. USERS ──────────────────────────────────────────────────────────────
section("1. Users")
cols, rows = fetchall(sc, "SELECT * FROM [Users]")
print(f"  Source: {len(rows)} rows")
ins = sk = 0
for row in rows:
    r = dict(zip(cols, row))
    dc.execute("SELECT COUNT(*) FROM Users WHERE Id=%s", (r['Id'],))
    if dc.fetchone()[0] > 0:
        sk += 1
        continue
    dc.execute("""
        INSERT INTO Users (
            Id,FirstName,LastName,PanNumber,Role,UserType,
            ApprovalStatus,ApprovedAt,ApprovedByUserId,RejectionReason,
            IsActive,CreatedAt,LastLoginAt,
            UserName,NormalizedUserName,Email,NormalizedEmail,
            EmailConfirmed,PasswordHash,SecurityStamp,ConcurrencyStamp,
            PhoneNumber,PhoneNumberConfirmed,TwoFactorEnabled,
            LockoutEnd,LockoutEnabled,AccessFailedCount
        ) VALUES (%s,%s,%s,%s,%s,%s, %s,%s,%s,%s, %s,%s,%s,
                  %s,%s,%s,%s, %s,%s,%s,%s, %s,%s,%s, %s,%s,%s)
    """, (
        s(r['Id']), s(r['FirstName']), s(r['LastName']), s(r['PanNumber']),
        r['Role'], r['UserType'],
        r['ApprovalStatus'], r['ApprovedAt'], s(r['ApprovedByUserId']),
        s(r['RejectionReason']),
        bool(r['IsActive']), r['CreatedAt'], r['LastLoginAt'],
        s(r['UserName']), s(r['NormalizedUserName']),
        s(r['Email']), s(r['NormalizedEmail']),
        bool(r['EmailConfirmed']), s(r['PasswordHash']),
        s(r['SecurityStamp']), s(r['ConcurrencyStamp']),
        s(r['PhoneNumber']), bool(r['PhoneNumberConfirmed']),
        bool(r['TwoFactorEnabled']),
        r['LockoutEnd'], bool(r['LockoutEnabled']), r['AccessFailedCount']
    ))
    ins += 1
dst.commit()
ok(f"Users: {ins} inserted, {sk} skipped")

# ─── 2. PERMISSIONS ────────────────────────────────────────────────────────
section("2. Permissions")
cols, rows = fetchall(sc, "SELECT * FROM [Permissions]")
print(f"  Source: {len(rows)} rows")
ins = sk = 0
for row in rows:
    r = dict(zip(cols, row))
    dc.execute("SELECT COUNT(*) FROM Permissions WHERE Id=%s", (r['Id'],))
    if dc.fetchone()[0] > 0:
        sk += 1; continue
    dc.execute(
        "INSERT INTO Permissions (Id,Code,Name,Description,CreatedAt) VALUES (%s,%s,%s,%s,%s)",
        (r['Id'], s(r['Code']), s(r['Name']), s(r['Description']), r['CreatedAt'])
    )
    ins += 1
dst.commit()
ok(f"Permissions: {ins} inserted, {sk} skipped")

# ─── 3. REFRESH TOKENS ─────────────────────────────────────────────────────
section("3. RefreshTokens")
cols, rows = fetchall(sc, "SELECT * FROM [RefreshTokens]")
print(f"  Source: {len(rows)} rows")
ins = sk = 0
for row in rows:
    r = dict(zip(cols, row))
    dc.execute("SELECT COUNT(*) FROM RefreshTokens WHERE Id=%s", (r['Id'],))
    if dc.fetchone()[0] > 0:
        sk += 1; continue
    dc.execute("""
        INSERT INTO RefreshTokens
            (Id,Token,UserId,ExpiresAt,CreatedAt,RevokedAt,
             ReplacedByToken,CreatedByIp,RevokedByIp)
        VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s)
    """, (
        r['Id'], s(r['Token']), s(r['UserId']),
        r['ExpiresAt'], r['CreatedAt'], r['RevokedAt'],
        s(r['ReplacedByToken']), s(r['CreatedByIp']), s(r['RevokedByIp'])
    ))
    ins += 1
dst.commit()
ok(f"RefreshTokens: {ins} inserted, {sk} skipped")

# ─── 4. USER PERMISSIONS ───────────────────────────────────────────────────
section("4. UserPermissions")
cols, rows = fetchall(sc, "SELECT * FROM [UserPermissions]")
print(f"  Source: {len(rows)} rows")
ins = sk = 0
for row in rows:
    r = dict(zip(cols, row))
    dc.execute("SELECT COUNT(*) FROM UserPermissions WHERE Id=%s", (r['Id'],))
    if dc.fetchone()[0] > 0:
        sk += 1; continue
    dc.execute("""
        INSERT INTO UserPermissions
            (Id,UserId,PermissionId,GrantedByUserId,GrantedAt,RevokedAt,RevokedByUserId)
        VALUES (%s,%s,%s,%s,%s,%s,%s)
    """, (
        r['Id'], s(r['UserId']), r['PermissionId'], s(r['GrantedByUserId']),
        r['GrantedAt'], r['RevokedAt'], s(r['RevokedByUserId'])
    ))
    ins += 1
dst.commit()
ok(f"UserPermissions: {ins} inserted, {sk} skipped")

# ─── 5. SCHEME ENROLLMENTS ─────────────────────────────────────────────────
section("5. SchemeEnrollments")
cols, rows = fetchall(sc, "SELECT * FROM [SchemeEnrollments]")
print(f"  Source: {len(rows)} rows")
ins = sk = 0
for row in rows:
    r = dict(zip(cols, row))
    dc.execute("SELECT COUNT(*) FROM SchemeEnrollments WHERE Id=%s", (r['Id'],))
    if dc.fetchone()[0] > 0:
        sk += 1; continue
    dc.execute("""
        INSERT INTO SchemeEnrollments
            (Id,SchemeCode,SchemeName,FundName,IsApproved,CreatedAt,UpdatedAt)
        VALUES (%s,%s,%s,%s,%s,%s,%s)
    """, (
        r['Id'], s(r['SchemeCode']), s(r['SchemeName']),
        s(r.get('FundName', '')), bool(r['IsApproved']),
        r['CreatedAt'], r.get('UpdatedAt')
    ))
    ins += 1
dst.commit()
ok(f"SchemeEnrollments: {ins} inserted, {sk} skipped")

# ─── 6. INVESTMENT ORDERS ──────────────────────────────────────────────────
section("6. InvestmentOrders")
cols, rows = fetchall(sc, "SELECT * FROM [InvestmentOrders] ORDER BY Id")
print(f"  Source: {len(rows)} rows")
ins = sk = er = 0
for row in rows:
    r = dict(zip(cols, row))
    dc.execute("SELECT COUNT(*) FROM InvestmentOrders WHERE Id=%s", (r['Id'],))
    if dc.fetchone()[0] > 0:
        sk += 1; continue
    try:
        dc.execute("""
            INSERT INTO InvestmentOrders (
                Id,OrderNumber,InvestorUserId,InvestorName,
                SchemeCode,SchemeName,FundName,
                InvestedAmount,PaymentMode,ChequeNumber,ChequeDate,
                BankName,TransactionRef,
                OrderDate,Status,
                AssignedDate,AssignedStaffName,
                SubmittedDate,SubmittedByUserId,
                VerifiedDate,VerifiedByUserId,
                PurchaseNAV,UnitsAllotted,FolioNumber,ActivatedDate,
                Notes,CreatedByUserId,CreatedAt,UpdatedAt
            ) VALUES (
                %s,%s,%s,%s, %s,%s,%s,
                %s,%s,%s,%s, %s,%s,
                %s,%s, %s,%s, %s,%s, %s,%s,
                %s,%s,%s,%s, %s,%s,%s,%s
            )
        """, (
            r['Id'], s(r['OrderNumber']),
            s(r['InvestorUserId']), s(r['InvestorName']),
            s(r['SchemeCode']), s(r['SchemeName']), s(r['FundName']),
            r['InvestedAmount'], s(r['PaymentMode']),
            s(r.get('ChequeNumber')), r.get('ChequeDate'),
            s(r.get('BankName')), s(r.get('TransactionRef')),
            r.get('OrderDate'), s(r.get('Status')),
            r.get('AssignedDate'), s(r.get('AssignedStaffName')),
            r.get('SubmittedDate'), s(r.get('SubmittedByUserId')),
            r.get('VerifiedDate'), s(r.get('VerifiedByUserId')),
            r.get('PurchaseNAV'), r.get('UnitsAllotted'),
            s(r.get('FolioNumber')), r.get('ActivatedDate'),
            s(r.get('Notes')), s(r.get('CreatedByUserId')),
            r.get('CreatedAt'), r.get('UpdatedAt')
        ))
        ins += 1
    except Exception as ex:
        err(f"Order {r['Id']} ({r.get('OrderNumber')}): {ex}")
        er += 1
dst.commit()
ok(f"InvestmentOrders: {ins} inserted, {sk} skipped, {er} errors")

# ─── 7. HOLDINGS ───────────────────────────────────────────────────────────
# SQL Server Holdings columns: Id,OrderId,InvestorUserId,InvestorName,
#   SchemeCode,SchemeName,FundName,FolioNumber,PurchaseDate,PurchaseNAV,
#   InvestedAmount,Units,IsActive,CreatedAt
# MySQL Holdings columns:      Id,OrderId,InvestorUserId,InvestorName,
#   SchemeCode,SchemeName,FundName,FolioNumber,PurchaseDate,PurchaseNAV,
#   InvestedAmount,Units,IsActive,CreatedAt   <-- same!
section("7. Holdings")
cols, rows = fetchall(sc, "SELECT * FROM [Holdings] ORDER BY Id")
print(f"  Source: {len(rows)} rows")
ins = sk = er = 0
for row in rows:
    r = dict(zip(cols, row))
    dc.execute("SELECT COUNT(*) FROM Holdings WHERE Id=%s", (r['Id'],))
    if dc.fetchone()[0] > 0:
        sk += 1; continue
    try:
        dc.execute("""
            INSERT INTO Holdings
                (Id,OrderId,InvestorUserId,InvestorName,
                 SchemeCode,SchemeName,FundName,FolioNumber,
                 PurchaseDate,PurchaseNAV,InvestedAmount,Units,
                 IsActive,CreatedAt)
            VALUES (%s,%s,%s,%s, %s,%s,%s,%s, %s,%s,%s,%s, %s,%s)
        """, (
            r['Id'], r['OrderId'],
            s(r['InvestorUserId']), s(r['InvestorName']),
            s(r['SchemeCode']), s(r['SchemeName']),
            s(r['FundName']), s(r.get('FolioNumber')),
            r.get('PurchaseDate'), r.get('PurchaseNAV'),
            r.get('InvestedAmount'), r.get('Units'),
            bool(r.get('IsActive', True)), r.get('CreatedAt')
        ))
        ins += 1
    except Exception as ex:
        err(f"Holding {r['Id']}: {ex}")
        er += 1
dst.commit()
ok(f"Holdings: {ins} inserted, {sk} skipped, {er} errors")

# ─── 8. PORTFOLIO SNAPSHOTS ────────────────────────────────────────────────
# MySQL PortfolioSnapshots: Id,HoldingId,InvestorUserId,InvestorName,
#   SchemeCode,SchemeName,FundName,SnapshotDate,CurrentNAV,InvestedAmount,
#   CurrentValue,ProfitLoss,ProfitLossPercent,CreatedAt
section("8. PortfolioSnapshots")
dc.execute("SELECT COUNT(*) FROM PortfolioSnapshots")
existing_cnt = dc.fetchone()[0]
print(f"  MySQL existing: {existing_cnt}")

if existing_cnt >= 1152:
    skip("Already fully populated - skipping")
else:
    cols, rows = fetchall(sc, "SELECT * FROM [PortfolioSnapshots] ORDER BY Id")
    print(f"  Source: {len(rows)} rows")

    dc.execute("SELECT Id FROM Holdings")
    valid_hids = {row[0] for row in dc.fetchall()}

    ins = sk = er = 0
    batch = []
    BATCH = 100

    for row in rows:
        r = dict(zip(cols, row))
        hid = r.get('HoldingId')
        if hid not in valid_hids:
            warn(f"  Snapshot {r['Id']}: HoldingId {hid} missing from Holdings - skip")
            sk += 1
            continue
        batch.append((
            r['Id'], hid,
            s(r['InvestorUserId']), s(r['InvestorName']),
            s(r['SchemeCode']), s(r['SchemeName']), s(r['FundName']),
            r['SnapshotDate'],
            r['CurrentNAV'], r['InvestedAmount'], r['CurrentValue'],
            r['ProfitLoss'], r['ProfitLossPercent'],
            r['CreatedAt']
        ))
        if len(batch) >= BATCH:
            dc.executemany("""
                INSERT IGNORE INTO PortfolioSnapshots
                    (Id,HoldingId,InvestorUserId,InvestorName,
                     SchemeCode,SchemeName,FundName,SnapshotDate,
                     CurrentNAV,InvestedAmount,CurrentValue,
                     ProfitLoss,ProfitLossPercent,CreatedAt)
                VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
            """, batch)
            ins += len(batch)
            dst.commit()
            print(f"  ... {ins} snapshots written")
            batch = []

    if batch:
        dc.executemany("""
            INSERT IGNORE INTO PortfolioSnapshots
                (Id,HoldingId,InvestorUserId,InvestorName,
                 SchemeCode,SchemeName,FundName,SnapshotDate,
                 CurrentNAV,InvestedAmount,CurrentValue,
                 ProfitLoss,ProfitLossPercent,CreatedAt)
            VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
        """, batch)
        ins += len(batch)
        dst.commit()

    ok(f"PortfolioSnapshots: {ins} inserted, {sk} skipped, {er} errors")

# ─── 9. JOB EXECUTION LOGS ─────────────────────────────────────────────────
section("9. JobExecutionLogs")
cols, rows = fetchall(sc, "SELECT * FROM [JobExecutionLogs] ORDER BY Id")
print(f"  Source: {len(rows)} rows")
ins = sk = 0
for row in rows:
    r = dict(zip(cols, row))
    dc.execute("SELECT COUNT(*) FROM JobExecutionLogs WHERE Id=%s", (r['Id'],))
    if dc.fetchone()[0] > 0:
        sk += 1; continue
    dc.execute("""
        INSERT INTO JobExecutionLogs
            (Id,JobName,StartedAt,CompletedAt,IsSuccess,
             ErrorMessage,Details,ElapsedSeconds,CreatedAt,UpdatedAt)
        VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
    """, (
        r['Id'], s(r['JobName']),
        r.get('StartedAt'), r.get('CompletedAt'),
        bool(r['IsSuccess']), s(r.get('ErrorMessage')),
        s(r.get('Details')), r.get('ElapsedSeconds'),
        r.get('CreatedAt'), r.get('UpdatedAt')
    ))
    ins += 1
dst.commit()
ok(f"JobExecutionLogs: {ins} inserted, {sk} skipped")

# ─── VERIFICATION TABLE ─────────────────────────────────────────────────────
section("VERIFICATION - Row Count Comparison")
checks = [
    ('Users', 'Users'),
    ('Permissions', 'Permissions'),
    ('UserPermissions', 'UserPermissions'),
    ('RefreshTokens', 'RefreshTokens'),
    ('SchemeEnrollments', 'SchemeEnrollments'),
    ('InvestmentOrders', 'InvestmentOrders'),
    ('Holdings', 'Holdings'),
    ('PortfolioSnapshots', 'PortfolioSnapshots'),
    ('JobExecutionLogs', 'JobExecutionLogs'),
]

print(f"\n  {'Table':<30} {'SQL Server':>12} {'MySQL':>12}  Status")
print(f"  {'-'*60}")
all_ok = True
for ss_tbl, my_tbl in checks:
    sc.execute(f"SELECT COUNT(*) FROM [{ss_tbl}]")
    ss_c = sc.fetchone()[0]
    dc.execute(f"SELECT COUNT(*) FROM {my_tbl}")
    my_c = dc.fetchone()[0]
    status = "OK" if my_c >= ss_c else "MISMATCH"
    if status != "OK":
        all_ok = False
    print(f"  {status:<4}  {my_tbl:<28} {ss_c:>12} {my_c:>12}")

dc.execute("SET FOREIGN_KEY_CHECKS=1")
dst.commit()
src.close()
dst.close()

print(f"\n{'='*58}")
if all_ok:
    print("  Migration complete! All row counts match.")
else:
    print("  Migration done with warnings. Check MISMATCH rows above.")
print(f"{'='*58}\n")
