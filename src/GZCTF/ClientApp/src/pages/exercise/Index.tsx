import {
  Badge,
  Button,
  Card,
  Center,
  Divider,
  Group,
  ScrollArea,
  SimpleGrid,
  Skeleton,
  Stack,
  Switch,
  Tabs,
  Text,
  Title,
} from '@mantine/core'
import { useLocalStorage } from '@mantine/hooks'
import { mdiPuzzle, mdiTrophyOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import cx from 'clsx'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { Empty } from '@Components/Empty'
import { ExerciseChallengeModal } from '@Components/ExerciseChallengeModal'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { useChallengeCategoryLabelMap } from '@Utils/Shared'
import { useExerciseChallenges } from '@Hooks/useExercise'
import { usePageTitle } from '@Hooks/usePageTitle'
import { ChallengeCategory, ExerciseInfoModel, Role } from '@Api'
import classes from '@Styles/ChallengePanel.module.css'
import misc from '@Styles/Misc.module.css'

const Exercise: FC = () => {
  const { t } = useTranslation()
  usePageTitle(t('exercise.title'))

  const { challenges } = useExerciseChallenges()

  const categories = Object.keys(challenges ?? {})
  const [activeTab, setActiveTab] = useState<ChallengeCategory | 'All'>('All')
  const [hideSolved, setHideSolved] = useLocalStorage({
    key: 'exercise-hide-solved',
    defaultValue: false,
    getInitialValueInEffect: false,
  })
  const challengeCategoryLabelMap = useChallengeCategoryLabelMap()

  const allChallenges = Object.values(challenges ?? {}).flat()

  const currentChallenges =
    challenges &&
    (activeTab !== 'All' ? (challenges[activeTab] ?? []) : allChallenges).filter(
      (chal) => !hideSolved || !chal.isSolved
    )

  const [challenge, setChallenge] = useState<ExerciseInfoModel | null>(null)
  const [detailOpened, setDetailOpened] = useState(false)

  // skeleton for loading
  if (!challenges) {
    return (
      <WithNavBar width="90%">
        <WithRole requiredRole={Role.User}>
          <Stack gap="md">
            <Group gap="sm" justify="space-between" align="center" wrap="nowrap">
              <Stack gap={0}>
                <Skeleton height="2rem" width="8rem" />
                <Skeleton height="1rem" width="18rem" mt={6} />
              </Stack>
              <Skeleton height="2rem" width="8rem" />
            </Group>
            <Group gap="sm" align="flex-start" wrap="nowrap">
              <Stack miw="10rem" maw="10rem">
                {Array(6)
                  .fill(null)
                  .map((_v, i) => (
                    <Group key={i} wrap="nowrap" p={10}>
                      <Skeleton height="1.5rem" width="1.5rem" />
                      <Skeleton height="1rem" />
                    </Group>
                  ))}
              </Stack>
              <SimpleGrid p="xs" spacing="sm" pos="relative" w="100%" cols={{ base: 3, w18: 4, w24: 6, w30: 8 }}>
                {Array(8)
                  .fill(null)
                  .map((_v, i) => (
                    <Card key={i} shadow="sm">
                      <Stack gap="sm" pos="relative" style={{ zIndex: 99 }}>
                        <Skeleton height="1.5rem" width="70%" mt={4} />
                        <Divider />
                        <Skeleton height="1.5rem" width="5rem" />
                      </Stack>
                    </Card>
                  ))}
              </SimpleGrid>
            </Group>
          </Stack>
        </WithRole>
      </WithNavBar>
    )
  }

  if (allChallenges.length === 0) {
    return (
      <WithNavBar width="90%">
        <WithRole requiredRole={Role.User}>
          <Center h="calc(100vh - 100px)" w="100%">
            <Empty bordered description={t('exercise.empty')} fontSize="xl" mdiPath={mdiPuzzle} iconSize={8} />
          </Center>
        </WithRole>
      </WithNavBar>
    )
  }

  return (
    <WithNavBar width="90%">
      <WithRole requiredRole={Role.User}>
        <Group gap="sm" justify="space-between" align="center" wrap="nowrap" px="md" pt="md">
          <Stack gap={0}>
            <Title order={2}>{t('exercise.title')}</Title>
          </Stack>
          <Button
            component={Link}
            to="/exercise/scoreboard"
            leftSection={<Icon path={mdiTrophyOutline} size={1} />}
          >
            {t('exercise.button.scoreboard')}
          </Button>
        </Group>

        <Group gap="sm" justify="space-between" align="flex-start" wrap="nowrap">
          <Stack miw="10.5rem">
            <Switch
              w="10.5rem"
              checked={hideSolved}
              onChange={(e) => setHideSolved(e.target.checked)}
              classNames={{ body: classes.switch }}
              label={
                <Text fz="md" fw="bold" ta="right">
                  {t('exercise.button.hide_solved')}
                </Text>
              }
            />
            <Tabs
              orientation="vertical"
              variant="pills"
              value={activeTab}
              onChange={(value) => setActiveTab(value as ChallengeCategory)}
              classNames={{
                root: classes.tabRoot,
                list: classes.tabList,
                tabLabel: classes.tabLabel,
                tab: classes.tab,
              }}
            >
              <Tabs.List>
                <Tabs.Tab value={'All'} leftSection={<Icon path={mdiPuzzle} size={1} />}>
                  <Group justify="space-between" wrap="nowrap" gap={2}>
                    <Text fz="sm" fw="bold">
                      {t('exercise.tab.all')}
                    </Text>
                    <Text fz="sm" fw="bold">
                      {allChallenges.length}
                    </Text>
                  </Group>
                </Tabs.Tab>
                {categories.map((tab) => {
                  const data = challengeCategoryLabelMap.get(tab as ChallengeCategory)!
                  return (
                    <Tabs.Tab
                      key={tab}
                      value={tab}
                      leftSection={<Icon path={data?.icon} size={1} />}
                      color={data?.color}
                    >
                      <Group justify="space-between" wrap="nowrap" gap={2}>
                        <Text fz="sm" fw="bold">
                          {data?.name}
                        </Text>
                        <Text fz="sm" fw="bold">
                          {challenges && challenges[tab].length}
                        </Text>
                      </Group>
                    </Tabs.Tab>
                  )
                })}
              </Tabs.List>
            </Tabs>
          </Stack>

          <ScrollArea
            h="calc(100vh - 9rem)"
            pos="relative"
            offsetScrollbars
            scrollbarSize={4}
            classNames={{ root: classes.scrollArea }}
          >
            {currentChallenges && currentChallenges.length ? (
              <SimpleGrid
                p="xs"
                w="100%"
                pt={0}
                spacing="sm"
                cols={{ base: 3, w18: 4, w24: 6, w30: 8, w36: 10, w42: 12, w48: 14 }}
              >
                {currentChallenges?.map((chal) => {
                  const cateData = challengeCategoryLabelMap.get(chal.category)
                  return (
                    <Card
                      key={chal.id}
                      onClick={() => {
                        setChallenge(chal)
                        setDetailOpened(true)
                      }}
                      shadow="sm"
                      className={cx(misc.hoverCard)}
                      data-faded={chal.isSolved || undefined}
                      data-no-move
                    >
                      <Stack gap="xs" pos="relative" style={{ zIndex: 99 }}>
                        <Group h="30px" wrap="nowrap" justify="space-between" gap={2}>
                          <Text fw="bold" lineClamp={1}>
                            {chal.title}
                          </Text>
                        </Group>
                        <Divider size="sm" color={cateData?.color} />
                        <Group wrap="nowrap" justify="space-between" align="center" gap={2}>
                          <Text ta="center" fw="bold" fz="lg" ff="monospace">
                            {chal.score}&nbsp;pts
                          </Text>
                          <Stack gap={2} align="flex-end">
                            {chal.isSolved && (
                              <Badge color="teal" size="sm">
                                {t('exercise.card.solved')}
                              </Badge>
                            )}
                            <Text fz="xs" c="dimmed">
                              {t('exercise.card.accepted_count', { count: chal.acceptedCount })}
                            </Text>
                            <Text fz="xs" c="dimmed">
                              {t('exercise.card.submissions', { count: chal.submissionCount })}
                            </Text>
                          </Stack>
                        </Group>
                      </Stack>
                    </Card>
                  )
                })}
              </SimpleGrid>
            ) : (
              <Center h="calc(100vh - 12rem)">
                <Stack gap={0} align="center">
                  <Title order={3}>{t('exercise.tab.all')}</Title>
                  <Text c="dimmed">{t('exercise.empty')}</Text>
                </Stack>
              </Center>
            )}
          </ScrollArea>
        </Group>

        {challenge && (
          <ExerciseChallengeModal
            opened={detailOpened}
            withCloseButton={false}
            onClose={() => setDetailOpened(false)}
            exerciseId={challenge.id!}
            title={challenge.title}
            score={challenge.score}
            solved={challenge.isSolved}
            cateData={challengeCategoryLabelMap.get(challenge.category ?? ChallengeCategory.Misc)!}
          />
        )}
      </WithRole>
    </WithNavBar>
  )
}

export default Exercise
