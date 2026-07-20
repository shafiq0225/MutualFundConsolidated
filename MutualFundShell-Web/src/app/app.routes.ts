import { Routes } from '@angular/router';
import { PlaceholderComponent } from './features/placeholder/placeholder.component';
import { authGuard, requiresPermission } from './core/guards/auth.guard';

// Import real components
import { LoginComponent } from './features/login/login.component';
import { RegisterComponent } from './features/register/register.component';
import { SchemesComponent } from './features/schemes/schemes.component';
import { NavComponent } from './features/nav/nav.component';
import { SchemeDetailsComponent } from './features/scheme-details/scheme-details.component';
import { UsersComponent } from './features/users/users.component';
import { PendingComponent } from './features/users/pending/pending.component';
import { FamilyComponent } from './features/family/family.component';
import { OrdersComponent } from './features/orders/orders.component';
import { InvestorComponent } from './features/investor/investor.component';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },

  { path: 'login', component: LoginComponent, title: 'Login' },
  { path: 'register', component: RegisterComponent, title: 'Register' },

  { path: 'scheme', component: SchemesComponent, title: 'Scheme Management', canActivate: [authGuard, requiresPermission('scheme.manage')] },
  { path: 'nav-comparison', component: NavComponent, title: 'NAV Comparison', canActivate: [authGuard] },
  { path: 'nav-comparison/scheme/:schemeCode', component: SchemeDetailsComponent, title: 'Scheme Details', canActivate: [authGuard] },
  { path: 'user', component: UsersComponent, title: 'User', canActivate: [authGuard, requiresPermission('user.manage')] },
  { path: 'pending-approvals', component: PendingComponent, title: 'Pending Approvals', canActivate: [authGuard, requiresPermission('user.manage')] },
  { path: 'family-groups', component: FamilyComponent, title: 'Family Groups', canActivate: [authGuard, requiresPermission('family.manage')] },
  
  { path: 'orders', component: OrdersComponent, title: 'Orders', canActivate: [authGuard, requiresPermission('order.view')] },
  { path: 'portfolio', component: InvestorComponent, title: 'Portfolio', canActivate: [authGuard, requiresPermission('investor.view')] },

  { path: 'dashboard', component: PlaceholderComponent, data: { title: 'Dashboard' }, title: 'Dashboard', canActivate: [authGuard] },

  { path: '**', redirectTo: 'dashboard' }
];
