import { Component, EventEmitter, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface RegisterDto {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  confirmPassword: string;
  panNumber: string;
}

export interface RegisterResponseDto {
  id: string;
  fullName: string;
  email: string;
  status: string;
  message: string;
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent {
  // Router is optional for MFE integration - when embedded as auth-register-element,
  // it emits registerSuccess/switchToLogin outputs instead of using router directly
  @Output() registerSuccess = new EventEmitter<void>();
  @Output() switchToLogin = new EventEmitter<void>();

  registerForm: FormGroup;
  isLoading = false;
  showPassword = false;
  showConfirmPassword = false;
  private readonly authApi = `${environment.authApiUrl}/api/auth`;

  private readonly router = inject(Router, { optional: true });

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private toastr: ToastrService
  ) {
    this.registerForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]],
      panNumber: ['', [Validators.required, Validators.pattern(/^[a-zA-Z]{5}[0-9]{4}[a-zA-Z]{1}$/)]]
    }, { validators: this.passwordMatchValidator });
  }

  passwordMatchValidator(g: FormGroup): { [key: string]: boolean } | null {
    return g.get('password')?.value === g.get('confirmPassword')?.value
      ? null
      : { mismatch: true };
  }

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    const registerDto: RegisterDto = {
      ...this.registerForm.value,
      panNumber: this.registerForm.value.panNumber?.trim()?.toUpperCase()
    };

    this.http.post<RegisterResponseDto>(`${this.authApi}/register`, registerDto).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.toastr.success(response.message || 'Registration successful. Please wait for admin approval.');
        setTimeout(() => {
          if (this.router) {
            this.router.navigate(['/login']);
          }
          this.registerSuccess.emit();
        }, 1500);
      },
      error: (error) => {
        this.isLoading = false;
        this.toastr.error(error.error?.message || 'Registration failed. Please try again.');
      },
      complete: () => {
        this.isLoading = false;
      }
    });
  }

  goToLogin(): void {
    if (this.router) {
      this.router.navigate(['/login']);
    }
    this.switchToLogin.emit();
  }
}
