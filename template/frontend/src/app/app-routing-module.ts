import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SalesListComponent } from './features/sales/sales-list/sales-list';
import { SalesCreateComponent } from './features/sales/sales-create/sales-create';
import { SalesDetailComponent } from './features/sales/sales-detail/sales-detail';
import { LoginComponent } from './features/auth/login/login';
import { AuthGuard } from './core/guards/auth.guard';

const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'sales', component: SalesListComponent, canActivate: [AuthGuard] },
  { path: 'sales/new', component: SalesCreateComponent, canActivate: [AuthGuard] },
  { path: 'sales/:id', component: SalesDetailComponent, canActivate: [AuthGuard] },
  { path: '', redirectTo: '/sales', pathMatch: 'full' },
  { path: '**', redirectTo: '/sales' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
