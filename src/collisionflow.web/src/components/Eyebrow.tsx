interface Props {
  children: string
  /** Renders in muted grey instead of the accent, for secondary labels. */
  quiet?: boolean
}

/**
 * The bracketed monospace label that opens every section.
 *
 * The brackets are real spans marked `aria-hidden` rather than CSS `content`.
 * Several screen readers do announce generated content, and "left square
 * bracket the shop floor right square bracket" is not what anyone needs to
 * hear — but the brackets also cannot simply be typed into the string,
 * because then they would be unavoidable. Marking them decorative in the DOM
 * is the only version that is both drawn and silent.
 */
export function Eyebrow({ children, quiet = false }: Props) {
  return (
    <p className={quiet ? 'eyebrow eyebrow--quiet' : 'eyebrow'}>
      <span className="eyebrow__bracket" aria-hidden="true">
        [
      </span>
      {children}
      <span className="eyebrow__bracket" aria-hidden="true">
        ]
      </span>
    </p>
  )
}
