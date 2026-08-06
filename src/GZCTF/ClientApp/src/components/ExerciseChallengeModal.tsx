import { ModalProps } from '@mantine/core'
import { useInputState } from '@mantine/hooks'
import { notifications, showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose, mdiLoading } from '@mdi/js'
import { Icon } from '@mdi/react'
import React, { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { ChallengeModal } from '@Components/ChallengeModal'
import { encryptApiData } from '@Utils/Crypto'
import { showErrorMsg } from '@Utils/Shared'
import { ChallengeCategoryItemProps } from '@Utils/Shared'
import { useConfig } from '@Hooks/useConfig'
import api, { AnswerResult, ChallengeDetailModel, ChallengeType } from '@Api'

interface ExerciseChallengeModalProps extends ModalProps {
  exerciseId: number
  cateData: ChallengeCategoryItemProps
  title: string
  score: number
  solved: boolean
}

export const ExerciseChallengeModal: FC<ExerciseChallengeModalProps> = (props) => {
  const { exerciseId, cateData, title, score, solved: initialSolved, ...modalProps } = props

  const { data: challenge, mutate } = api.exercise.useExerciseGetChallenge(exerciseId, {
    refreshInterval: 120 * 1000,
  })

  const { config } = useConfig()
  const { t } = useTranslation()

  const wrongFlagHints = t('challenge.content.wrong_flag_hints', {
    returnObjects: true,
  }) as string[]

  const isDynamic = challenge?.type === ChallengeType.DynamicContainer

  const [disabled, setDisabled] = useState(false)
  const [flag, setFlag] = useInputState('')
  const [solved, setSolved] = useState(initialSolved)

  const isLimitReached =
    (challenge?.submissionLimit && (challenge.attempts ?? 0) >= challenge.submissionLimit) || false

  const onSolved = () => {
    setSolved(true)
    setFlag('')
    mutate(challenge ? { ...challenge, isSolved: true } : undefined)
    // refresh the challenge list and scoreboard cache
    api.exercise.mutateExerciseGetChallenges()
    api.exercise.mutateExerciseGetScoreboard()
  }

  const onCreate = async () => {
    if (!exerciseId || disabled) return
    setDisabled(true)

    try {
      const res = await api.exercise.exerciseCreateContainer(exerciseId)
      mutate(
        challenge
          ? {
              ...challenge,
              context: {
                ...challenge.context,
                closeTime: res.data.expectStopAt,
                instanceEntry: res.data.entry,
              },
            }
          : undefined
      )
      showNotification({
        color: 'teal',
        title: t('challenge.notification.instance.created.title'),
        message: t('challenge.notification.instance.created.message'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setDisabled(false)
    }
  }

  const requestDestroy = async () => {
    try {
      await mutate()

      if (!challenge?.context?.instanceEntry) return

      await api.exercise.exerciseDeleteContainer(exerciseId)
      mutate(
        challenge
          ? {
              ...challenge,
              context: {
                ...challenge.context,
                closeTime: null,
                instanceEntry: null,
              },
            }
          : undefined
      )
      showNotification({
        color: 'teal',
        title: t('challenge.notification.instance.destroyed.title'),
        message: t('challenge.notification.instance.destroyed.message'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
    } catch (e) {
      showErrorMsg(e, t)
    }
  }

  const onDestroy = async () => {
    if (!exerciseId || disabled) return
    setDisabled(true)

    await requestDestroy()

    setDisabled(false)
  }

  const onExtend = async () => {
    if (!exerciseId || disabled) return
    setDisabled(true)

    try {
      const res = await api.exercise.exerciseExtendContainerLifetime(exerciseId)
      mutate(
        challenge
          ? {
              ...challenge,
              context: {
                ...challenge.context,
                closeTime: res.data.expectStopAt,
              },
            }
          : undefined
      )
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setDisabled(false)
    }
  }

  const onSubmit = async () => {
    if (!exerciseId || !flag) {
      showNotification({
        color: 'red',
        message: t('challenge.notification.flag.empty'),
        icon: <Icon path={mdiClose} size={1} />,
      })
      return
    }

    setDisabled(true)

    try {
      // the range submit returns the result synchronously, no polling needed
      const res = await api.exercise.exerciseSubmit(exerciseId, {
        flag: await encryptApiData(t, flag, config.apiPublicKey),
      })
      notifications.clean()
      setFlag('')
      setDisabled(false)

      if (res.data.status === AnswerResult.Accepted) {
        onSolved()
        showNotification({
          color: 'teal',
          title: t('challenge.notification.flag.accepted.title'),
          message: t('challenge.notification.flag.accepted.message'),
          icon: <Icon path={mdiCheck} size={1} />,
          autoClose: 8000,
        })
        if (isDynamic && challenge?.context?.instanceEntry) await requestDestroy()
        props.onClose()
      } else if (res.data.status === AnswerResult.WrongAnswer) {
        showNotification({
          color: 'red',
          title: t('challenge.notification.flag.wrong'),
          message: wrongFlagHints[Math.floor(Math.random() * wrongFlagHints.length)],
          icon: <Icon path={mdiClose} size={1} />,
          autoClose: 8000,
        })
      } else {
        showNotification({
          color: 'yellow',
          title: t('challenge.notification.flag.unknown.title'),
          message: t('challenge.notification.flag.unknown.message', {
            id: exerciseId,
          }),
          icon: <Icon path={mdiLoading} size={1} />,
          autoClose: false,
          withCloseButton: true,
        })
      }
    } catch (e) {
      showErrorMsg(e, t)
      setDisabled(false)
    }
  }

  return (
    <ChallengeModal
      {...modalProps}
      challenge={{ ...challenge, limit: challenge?.submissionLimit } as ChallengeDetailModel}
      cateData={cateData}
      solved={solved}
      flag={flag}
      setFlag={setFlag}
      onCreate={onCreate}
      onDestroy={onDestroy}
      onSubmitFlag={onSubmit}
      disabled={disabled || isLimitReached}
      onExtend={onExtend}
      gameTitle={t('exercise.title')}
    />
  )
}
