import { useEffect, useRef, useState, type ReactNode } from 'react'

interface Props {
  children: ReactNode
  /** Stagger, in milliseconds, for revealing a row of cards in sequence. */
  delay?: number
}

/**
 * Fades content in as it enters the viewport.
 *
 * Two deliberate restraints:
 *
 * Someone whose operating system reports `prefers-reduced-motion` has asked for less
 * motion for a reason - vestibular disorders make scroll-linked animation genuinely
 * unpleasant. The response is not a faster animation; it is no animation. The content is
 * simply already there.
 *
 * The observer disconnects after the first reveal. Re-animating every time an element
 * scrolls back into view reads as broken rather than polished, and it makes the page
 * impossible to skim.
 */
export function Reveal({ children, delay = 0 }: Props) {
  const ref = useRef<HTMLDivElement>(null)
  const [shown, setShown] = useState(false)

  useEffect(() => {
    const element = ref.current
    if (!element) {
      return
    }

    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      setShown(true)
      return
    }

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries.some((entry) => entry.isIntersecting)) {
          setShown(true)
          observer.disconnect()
        }
      },
      { rootMargin: '0px 0px -8% 0px' },
    )

    observer.observe(element)
    return () => observer.disconnect()
  }, [])

  return (
    <div
      ref={ref}
      className={shown ? 'reveal reveal--shown' : 'reveal'}
      style={{ transitionDelay: `${delay}ms` }}
    >
      {children}
    </div>
  )
}
