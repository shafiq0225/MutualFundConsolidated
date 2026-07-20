export interface JobLog {
  id: number;
  jobName: string;
  startedAt: string;
  completedAt: string | null;
  isSuccess: boolean;
  errorMessage: string | null;
  details: string | null;
  elapsedSeconds: number;
}
