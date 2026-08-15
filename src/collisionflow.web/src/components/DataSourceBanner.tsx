import type { SystemStatus } from '../types'

interface Props {
  status: SystemStatus | null
}

/**
 * Says plainly when the board is not backed by the real database.
 *
 * The free-tier database pauses when idle and takes up to a minute to wake. Rather than
 * show a spinner or an error, the application serves sample data and says so here. Hiding
 * that would be the worse choice: a user who believes a change was saved when it was not
 * is worse off than one who was told.
 *
 * role="status" rather than role="alert" - this is a condition to be aware of, not an
 * interruption, and it must not talk over whatever the user is doing.
 */
export function DataSourceBanner({ status }: Props) {
  if (!status?.isDegraded) {
    return null
  }

  return (
    <p className="banner" role="status">
      <strong>Demo mode.</strong>{' '}
      {status.message ?? 'The database is unavailable. Changes will not be saved.'}
    </p>
  )
}
