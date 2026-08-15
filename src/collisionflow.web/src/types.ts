/**
 * Status names mirror the server enum exactly. They travel as strings, so a
 * renumbering on the server can never silently change what the client displays.
 */
export type RepairStatus =
  | 'Received'
  | 'InProgress'
  | 'WaitingOnParts'
  | 'QualityCheck'
  | 'ReadyForPickup'
  | 'Completed'

export interface RepairJob {
  id: string
  jobNumber: string
  customerName: string
  vehicleYear: number
  vehicleMake: string
  vehicleModel: string
  vehicleDescription: string
  repairCenter: string
  status: RepairStatus
  statusDisplayName: string
  /** Where this job may legally go next. The server decides; the UI only renders. */
  allowedTransitions: RepairStatus[]
  createdUtc: string
  updatedUtc: string
}

export interface RepairStatusInfo {
  status: RepairStatus
  displayName: string
  sortOrder: number
  isTerminal: boolean
  allowedTransitions: RepairStatus[]
}

/** RFC 7807 problem document, plus the extensions this API adds. */
export interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
  currentStatus?: RepairStatus
  requestedStatus?: RepairStatus
  allowedTransitions?: RepairStatus[]
  traceId?: string
}
