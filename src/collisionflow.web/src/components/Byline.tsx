import { BracketLink } from './BracketLink'

const EMAIL = 'batov.gleb1@gmail.com'

interface Link {
  label: string
  href: string
  external: boolean
}

/* The address is shown as itself rather than as "Email": a visible address can be read
   off the screen and typed into any client, which a bare mailto link cannot. */
const LINKS: Link[] = [
  { label: 'batovgleb.com', href: 'https://batovgleb.com/', external: true },
  { label: 'LinkedIn', href: 'https://www.linkedin.com/in/glebbatov/', external: true },
  { label: 'GitHub', href: 'https://github.com/glebbatov', external: true },
  { label: EMAIL, href: `mailto:${EMAIL}`, external: false },
]

/**
 * Who built this, where a reader expects to find out.
 *
 * Editorial order is eyebrow, headline, standfirst, byline. Putting the author here rather
 * than in a bar of their own keeps the masthead to the two things it exists for — the
 * product's name and whether it is live — and still puts a name and four ways to reach it
 * above the fold on a laptop.
 */
export function Byline() {
  return (
    <div className="byline">
      <p className="byline__who">
        Built by <strong>Gleb Batov</strong>, Full Stack Software Developer
      </p>
      <ul className="byline__links" aria-label="Contact the developer">
        {LINKS.map((link) => (
          <li key={link.href}>
            <BracketLink href={link.href} variant="meta" external={link.external}>
              {link.label}
            </BracketLink>
          </li>
        ))}
      </ul>
    </div>
  )
}
