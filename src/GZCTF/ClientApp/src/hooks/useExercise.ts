import { OnceSWRConfig } from '@Hooks/useConfig'
import api from '@Api'

export const useExerciseChallenges = () => {
  const { data, error, mutate } = api.exercise.useExerciseGetChallenges({
    ...OnceSWRConfig,
    refreshInterval: 30 * 1000,
  })

  return { challenges: data, error, mutate }
}

export const useExerciseScoreboard = () => {
  const { data, error, mutate } = api.exercise.useExerciseGetScoreboard({
    ...OnceSWRConfig,
    refreshInterval: 60 * 1000,
  })

  return { scoreboard: data, error, mutate }
}
