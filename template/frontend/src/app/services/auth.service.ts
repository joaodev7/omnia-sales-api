import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { tap } from 'rxjs/operators';

export interface AuthResponse {
  success: boolean;
  message: string;
  data: {
    token: string;
    email: string;
    name: string;
    role: string;
  };
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = '/api/auth';
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(this.hasToken());

  constructor(private http: HttpClient) {}

  private hasToken(): boolean {
    return !!localStorage.getItem('jwt_token');
  }

  public get isAuthenticated$(): Observable<boolean> {
    return this.isAuthenticatedSubject.asObservable();
  }

  public get isAuthenticated(): boolean {
    return this.isAuthenticatedSubject.value;
  }

  public getToken(): string | null {
    return localStorage.getItem('jwt_token');
  }

  public getUserName(): string | null {
    return localStorage.getItem('user_name');
  }

  public login(email: string, password: string): Observable<any> {
    return this.http.post<any>(this.apiUrl, { email, password }).pipe(
      tap(response => {
        if (response && response.success) {
          // Case 1: Double-wrapped response (template's standard format)
          if (response.data && response.data.success && response.data.data && response.data.data.token) {
            const authData = response.data.data;
            localStorage.setItem('jwt_token', authData.token);
            localStorage.setItem('user_name', authData.name);
            localStorage.setItem('user_email', authData.email);
            localStorage.setItem('user_role', authData.role);
            this.isAuthenticatedSubject.next(true);
          }
          // Case 2: Standard single-wrapped format
          else if (response.data && response.data.token) {
            const authData = response.data;
            localStorage.setItem('jwt_token', authData.token);
            localStorage.setItem('user_name', authData.name);
            localStorage.setItem('user_email', authData.email);
            localStorage.setItem('user_role', authData.role);
            this.isAuthenticatedSubject.next(true);
          }
        }
      })
    );
  }

  public logout(): void {
    localStorage.removeItem('jwt_token');
    localStorage.removeItem('user_name');
    localStorage.removeItem('user_email');
    localStorage.removeItem('user_role');
    this.isAuthenticatedSubject.next(false);
  }
}
