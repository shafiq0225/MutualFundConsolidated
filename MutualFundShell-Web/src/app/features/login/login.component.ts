import { Component, EventEmitter, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { AuthService, LoginDto } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  // Router is only present when this component is routed to directly in
  // the standalone Auth-Web app (see app.routes.ts). When registered as
  // auth-login-element (see main.elements.ts) it's bootstrapped with no
  // router at all, so `inject` would throw — hence the optional injection
  // and the loginSuccess/switchToRegister outputs, which the shell's
  // LoginHostComponent listens for instead to drive its own router.
  @Output() loginSuccess = new EventEmitter<void>();
  @Output() switchToRegister = new EventEmitter<void>();

  loginForm: FormGroup;
  isLoading = false;
  showPassword = false;

  private readonly router = inject(Router, { optional: true });
  private readonly route = inject(ActivatedRoute);

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private toastr: ToastrService
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    const loginDto: LoginDto = this.loginForm.value;

    this.authService.login(loginDto).subscribe({
      next: () => {
        this.toastr.success('Login successful');
        if (this.loginSuccess.observers.length === 0) {
          const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/dashboard';
          this.router?.navigateByUrl(returnUrl);
        }
        this.loginSuccess.emit();
      },
      error: (error) => {
        this.isLoading = false;
        const code = error.error?.errorCode || error.error?.code;
        const msg = error.error?.message;

        if (code === 'ACCOUNT_PENDING') {
          this.toastr.warning(
            msg || 'Your account is pending admin approval. You will be notified once approved.',
            'Awaiting Approval',
            { timeOut: 6000 }
          );
        } else if (code === 'ACCOUNT_REJECTED') {
          this.toastr.error(
            msg || 'Your account registration was rejected.',
            'Account Rejected',
            { timeOut: 6000 }
          );
        } else {
          this.toastr.error(msg || 'Login failed. Please check your credentials.');
        }
      },
      complete: () => {
        this.isLoading = false;
      }
    });
  }

  goToRegister(): void {
    this.router?.navigate(['/register']);
    this.switchToRegister.emit();
  }
}
