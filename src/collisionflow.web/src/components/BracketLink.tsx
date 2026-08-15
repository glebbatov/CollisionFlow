interface Props {
  href: string
  children: string
  /** `nav` is an uppercase tracked label; `meta` is normal-case data such as a URL. */
  variant: 'nav' | 'meta'
  /** Opens in a new tab. Reserved for links that leave the site. */
  external?: boolean
}

/**
 * The bracketed link — the eyebrow's idiom made interactive.
 *
 * The brackets are real spans marked `aria-hidden`, for the same reason the eyebrow's are:
 * CSS `content` is announced by several screen readers, and "left square bracket board
 * right square bracket" is not a link name anyone needs to hear.
 *
 * External links carry `rel="noreferrer"`: without it the opened page receives a
 * `window.opener` handle back to this one. The new-tab behavior is also said out loud for
 * screen reader users, who otherwise discover it only after it has happened.
 */
export function BracketLink({ href, children, variant, external = false }: Props) {
  return (
    <a
      className={`bracketlink bracketlink--${variant}`}
      href={href}
      target={external ? '_blank' : undefined}
      rel={external ? 'noreferrer' : undefined}
    >
      <span className="bracketlink__bracket" aria-hidden="true">
        [
      </span>
      {children}
      {external && <span className="visually-hidden"> (opens in a new tab)</span>}
      <span className="bracketlink__bracket" aria-hidden="true">
        ]
      </span>
    </a>
  )
}
