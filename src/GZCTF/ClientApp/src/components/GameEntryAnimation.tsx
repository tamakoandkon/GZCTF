import { CSSProperties, FC, useEffect, useMemo, useState } from 'react'
import { LogoBox } from '@Components/LogoBox'
import { useConfig } from '@Hooks/useConfig'
import { consumeGameTransitionState } from '@Utils/gameTransition'
import classes from '@Styles/GameEntryAnimation.module.css'

interface GameEntryAnimationProps {
  gameId: number
  poster?: string | null
  onCompleted?: () => void
}

type Phase = 'idle' | 'expand' | 'logo' | 'exit' | 'done'

type IntroStyle = CSSProperties & {
  '--intro-top': string
  '--intro-left': string
  '--intro-width': string
  '--intro-height': string
}

export const GameEntryAnimation: FC<GameEntryAnimationProps> = ({ gameId, poster, onCompleted }) => {
  const { config } = useConfig()
  const [phase, setPhase] = useState<Phase>('idle')

  const transitionState = useMemo(() => consumeGameTransitionState(gameId), [gameId])

  const computedStyle = useMemo(() => {
    if (!transitionState) {
      return null
    }

    const { top, left, width, height } = transitionState

    const style: IntroStyle = {
      '--intro-top': `${top}px`,
      '--intro-left': `${left}px`,
      '--intro-width': `${width}px`,
      '--intro-height': `${height}px`,
    }

    return style
  }, [transitionState])

  useEffect(() => {
    if (!transitionState) {
      setPhase('done')
      return
    }

    setPhase('idle')
    const raf = requestAnimationFrame(() => {
      setPhase('expand')
    })

    const logoTimer = window.setTimeout(() => {
      setPhase('logo')
    }, 600)

    const exitTimer = window.setTimeout(() => {
      setPhase('exit')
    }, 1800)

    const doneTimer = window.setTimeout(() => {
      setPhase('done')
    }, 2000)

    return () => {
      cancelAnimationFrame(raf)
      window.clearTimeout(logoTimer)
      window.clearTimeout(exitTimer)
      window.clearTimeout(doneTimer)
    }
  }, [transitionState])

  useEffect(() => {
    if (phase === 'done' && onCompleted) {
      onCompleted()
    }
  }, [phase, onCompleted])

  if (!transitionState || !computedStyle || phase === 'done') {
    return null
  }

  const appliedPoster = poster ?? transitionState.poster ?? ''

  return (
    <div className={classes.overlay} data-phase={phase} style={computedStyle}>
      <div className={classes.poster} style={{ backgroundImage: appliedPoster ? `url("${appliedPoster}")` : undefined }} />
      <div className={classes.contentWrapper}>
        <div className={classes.logoWrapper}>
          <LogoBox size="7.5rem" className={classes.logo} />
        </div>
        <div className={classes.title}>{config?.title ?? ''}</div>
      </div>
    </div>
  )
}
