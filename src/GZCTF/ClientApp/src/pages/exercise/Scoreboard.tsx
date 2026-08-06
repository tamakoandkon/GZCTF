import { Avatar, Center, Group, ScrollArea, Stack, Table, Text, Title, useMantineTheme } from '@mantine/core'
import { mdiTrophyOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'
import { Empty } from '@Components/Empty'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { useLanguage } from '@Utils/I18n'
import { useExerciseScoreboard } from '@Hooks/useExercise'
import { usePageTitle } from '@Hooks/usePageTitle'
import { useUser } from '@Hooks/useUser'
import { Role } from '@Api'

const Scoreboard: FC = () => {
  const { t } = useTranslation()
  const theme = useMantineTheme()
  const { locale } = useLanguage()
  usePageTitle(t('exercise.scoreboard.title'))

  const { scoreboard } = useExerciseScoreboard()
  const { user } = useUser()

  const items = scoreboard?.items ?? []
  const myUserId = user?.userId

  return (
    <WithNavBar width="90%">
      <WithRole requiredRole={Role.User}>
        <Stack gap="md" px="md" pt="md" pb="2rem">
          <Group gap="sm" align="center" wrap="nowrap">
            <Icon path={mdiTrophyOutline} size={1.2} />
            <Title order={2}>{t('exercise.scoreboard.title')}</Title>
          </Group>

          {items.length === 0 ? (
            <Center h="calc(100vh - 12rem)" w="100%">
              <Empty bordered description={t('exercise.scoreboard.empty')} fontSize="xl" iconSize={6} />
            </Center>
          ) : (
            <ScrollArea offsetScrollbars scrollbarSize={4}>
              <Table highlightOnHover verticalSpacing="xs">
                <Table.Thead>
                  <Table.Tr>
                    <Table.Th w="5rem">{t('exercise.scoreboard.rank')}</Table.Th>
                    <Table.Th>{t('exercise.scoreboard.user')}</Table.Th>
                    <Table.Th ta="right">{t('exercise.scoreboard.score')}</Table.Th>
                    <Table.Th ta="right">{t('exercise.scoreboard.solved_count')}</Table.Th>
                    <Table.Th ta="right">{t('exercise.scoreboard.last_solve_time')}</Table.Th>
                  </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                  {items.map((item) => {
                    const isMe = item.userId === myUserId
                    return (
                      <Table.Tr key={item.userId} bg={isMe ? theme.colors.brand[0] : undefined}>
                        <Table.Td>
                          <Text fw="bold" ff="monospace">
                            #{item.rank}
                          </Text>
                        </Table.Td>
                        <Table.Td>
                          <Group gap="xs" wrap="nowrap">
                            <Avatar alt="avatar" src={item.avatar ?? undefined} radius="md" size="sm">
                              {item.userName?.slice(0, 1) ?? 'U'}
                            </Avatar>
                            <Text fw={isMe ? 700 : 500}>{item.userName}</Text>
                          </Group>
                        </Table.Td>
                        <Table.Td ta="right">
                          <Text fw="bold" ff="monospace">
                            {item.score}
                          </Text>
                        </Table.Td>
                        <Table.Td ta="right">{item.solvedCount}</Table.Td>
                        <Table.Td ta="right">
                          <Text ff="monospace" size="sm">
                            {dayjs(item.lastSolveTime).locale(locale).format('L LTS')}
                          </Text>
                        </Table.Td>
                      </Table.Tr>
                    )
                  })}
                </Table.Tbody>
              </Table>
            </ScrollArea>
          )}
        </Stack>
      </WithRole>
    </WithNavBar>
  )
}

export default Scoreboard
