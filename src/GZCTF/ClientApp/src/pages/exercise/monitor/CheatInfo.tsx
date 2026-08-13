import {
  Accordion,
  Badge,
  Group,
  Paper,
  ScrollArea,
  SegmentedControl,
  Stack,
  Table,
  Text,
  useMantineColorScheme,
  useMantineTheme,
} from '@mantine/core'
import {
  mdiCheck,
  mdiClose,
  mdiCrosshairsQuestion,
  mdiDotsHorizontal,
  mdiExclamationThick,
  mdiFlag,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import { useLocalStorage } from '@mantine/hooks'
import dayjs from 'dayjs'
import { FC, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Empty } from '@Components/Empty'
import { WithExerciseMonitor } from '@Components/WithExerciseMonitor'
import { useLanguage } from '@Utils/I18n'
import api, { AnswerResult, ExerciseCheatInfoModel } from '@Api'

const AnswerResultIconMap = (size: number) => {
  const theme = useMantineTheme()
  const { colorScheme } = useMantineColorScheme()

  const colorIdx = colorScheme === 'dark' ? 4 : 7

  return new Map([
    [AnswerResult.Accepted, { path: mdiCheck, size, color: theme.colors.green[colorIdx] }],
    [AnswerResult.WrongAnswer, { path: mdiClose, size, color: theme.colors.red[colorIdx] }],
    [AnswerResult.NotFound, { path: mdiCrosshairsQuestion, size, color: theme.colors.gray[colorIdx] }],
    [AnswerResult.CheatDetected, { path: mdiExclamationThick, size, color: theme.colors.orange[colorIdx] }],
    [AnswerResult.FlagSubmitted, { path: mdiDotsHorizontal, size, color: theme.colors.gray[colorIdx] }],
  ])
}

interface CheatSubmissionInfo {
  time?: number
  submitUser: string
  ownedUser: string
  challenge: string
  answer: string
  status?: AnswerResult
}

const CheatInfo: FC = () => {
  const { data } = api.exercise.useExerciseCheatInfo()

  const [view, setView] = useLocalStorage<'team' | 'table'>({
    key: 'exercise-cheat-info-view',
    defaultValue: 'table',
    getInitialValueInEffect: false,
  })

  const { t } = useTranslation()
  const { locale } = useLanguage()
  const iconMap = AnswerResultIconMap(0.8)

  const items = useMemo<CheatSubmissionInfo[]>(
    () =>
      (data ?? []).map((info: ExerciseCheatInfoModel) => ({
        time: info.submission?.time,
        submitUser: info.submitUser?.userName ?? '',
        ownedUser: info.ownedUser?.userName ?? '',
        challenge: info.submission?.challenge ?? '',
        answer: info.submission?.answer ?? '',
        status: info.submission?.status,
      })),
    [data]
  )

  const bySubmitUser = useMemo(() => {
    const map = new Map<string, CheatSubmissionInfo[]>()
    items.forEach((item) => {
      const list = map.get(item.submitUser) ?? []
      list.push(item)
      map.set(item.submitUser, list)
    })
    return [...map.entries()]
  }, [items])

  if (!data) {
    return (
      <WithExerciseMonitor isLoading>
        <Stack gap="xs" w="100%" />
      </WithExerciseMonitor>
    )
  }

  return (
    <WithExerciseMonitor isLoading={!data}>
      <Group justify="space-between" w="100%">
        <SegmentedControl
          value={view}
          bg="transparent"
          onChange={(value) => setView(value as 'team' | 'table')}
          data={[
            { label: t('exercise.content.team_view.table'), value: 'table' },
            { label: t('exercise.content.team_view.accordion'), value: 'team' },
          ]}
        />
      </Group>
      {items.length === 0 ? (
        <Empty
          description={t('exercise.content.no_cheat.title')}
          fontSize="xl"
          mdiPath={mdiFlag}
          iconSize={8}
        />
      ) : view === 'table' ? (
        <Paper shadow="md" p="md">
          <ScrollArea offsetScrollbars h="calc(100vh - 200px)">
            <Table>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th w="0.6rem">
                    <Group align="center">
                      <Icon path={mdiFlag} size={0.8} />
                    </Group>
                  </Table.Th>
                  <Table.Th>{t('common.label.time')}</Table.Th>
                  <Table.Th>{t('exercise.label.cheat_info.submit_user')}</Table.Th>
                  <Table.Th>{t('exercise.label.cheat_info.owned_user')}</Table.Th>
                  <Table.Th>{t('common.label.challenge')}</Table.Th>
                  <Table.Th ff="monospace">{t('common.label.flag')}</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {items.map((item, idx) => (
                  <Table.Tr key={`${item.time}@${idx}`}>
                    <Table.Td>
                      <Icon {...iconMap.get(item.status ?? AnswerResult.FlagSubmitted)!} />
                    </Table.Td>
                    <Table.Td ff="monospace">
                      <Badge size="sm" color="indigo" fullWidth>
                        {dayjs(item.time).locale(locale).format('SL HH:mm:ss')}
                      </Badge>
                    </Table.Td>
                    <Table.Td>
                      <Text ff="monospace" size="sm" fw="bold">
                        {item.submitUser}
                      </Text>
                    </Table.Td>
                    <Table.Td>
                      <Text ff="monospace" size="sm" fw="bold">
                        {item.ownedUser}
                      </Text>
                    </Table.Td>
                    <Table.Td>{item.challenge}</Table.Td>
                    <Table.Td>
                      <Text ff="monospace" size="sm">
                        {item.answer}
                      </Text>
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          </ScrollArea>
        </Paper>
      ) : (
        <Paper shadow="md" p="md">
          <ScrollArea offsetScrollbars h="calc(100vh - 200px)">
            <Accordion>
              {bySubmitUser.map(([user, subs]) => (
                <Accordion.Item key={user} value={user}>
                  <Accordion.Control>
                    <Text ff="monospace" fw="bold">
                      {user}
                    </Text>
                  </Accordion.Control>
                  <Accordion.Panel>
                    <Stack gap="xs">
                      {subs.map((item, idx) => (
                        <Group key={`${item.time}@${idx}`} wrap="nowrap" justify="space-between" gap="sm">
                          <Group gap="sm" wrap="nowrap">
                            <Icon {...iconMap.get(item.status ?? AnswerResult.FlagSubmitted)!} />
                            <Text ff="monospace" size="sm">
                              {item.challenge}
                            </Text>
                            <Text size="sm" c="dimmed">
                              {t('exercise.label.cheat_info.owned_user')}: {item.ownedUser}
                            </Text>
                          </Group>
                          <Text size="xs" c="dimmed">
                            {dayjs(item.time).locale(locale).format('SL LTS')}
                          </Text>
                        </Group>
                      ))}
                    </Stack>
                  </Accordion.Panel>
                </Accordion.Item>
              ))}
            </Accordion>
          </ScrollArea>
        </Paper>
      )}
    </WithExerciseMonitor>
  )
}

export default CheatInfo
