import type {
  ProblemDetails,
  RepairJob,
  RepairStatus,
  RepairStatusInfo,
  StatusChange,
  SystemStatus,
} from '../types'

/**
 * Carries the server's problem document rather than flattening it to a string,
 * so the UI can show what the caller is actually allowed to do next.
 */
export class ApiError extends Error {
  readonly problem: ProblemDetails

  constructor(problem: ProblemDetails, fallbackMessage: string) {
    super(problem.detail ?? problem.title ?? fallbackMessage)
    this.name = 'ApiError'
    this.problem = problem
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`/api${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...init,
  })

  if (!response.ok) {
    let problem: ProblemDetails = { status: response.status }
    try {
      problem = { ...problem, ...(await response.json()) }
    } catch {
      // A non-JSON error body (a proxy timeout, say) still needs to surface
      // as something the user can read.
    }
    throw new ApiError(problem, `Request failed with status ${response.status}.`)
  }

  return (await response.json()) as T
}

export const api = {
  getRepairJobs: () => request<RepairJob[]>('/repair-jobs'),

  getStatuses: () => request<RepairStatusInfo[]>('/statuses'),

  getSystemStatus: () => request<SystemStatus>('/system/status'),

  getHistory: (id: string) => request<StatusChange[]>(`/repair-jobs/${id}/history`),

  updateStatus: (id: string, status: RepairStatus) =>
    request<RepairJob>(`/repair-jobs/${id}/status`, {
      method: 'PUT',
      body: JSON.stringify({ status }),
    }),
}
