import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { ShellRoot } from './app/app';

bootstrapApplication(ShellRoot, appConfig)
  .catch((err) => console.error(err));
