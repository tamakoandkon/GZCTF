import {
  Badge,
  Button,
  ComboboxItem,
  Grid,
  Group,
  Input,
  NumberInput,
  Select,
  Stack,
  Switch,
  Text,
  Textarea,
  TextInput,
  Title,
} from '@mantine/core'
import { DateTimePicker } from '@mantine/dates'
import { useModals } from '@mantine/modals'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiContentSaveOutline, mdiDeleteOutline, mdiKeyboardBackspace, mdiLinkVariant } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useParams } from 'react-router'
import { HintList } from '@Components/HintList'
import { InstanceEntry } from '@Components/InstanceEntry'
import { SwitchLabel } from '@Components/admin/SwitchLabel'
import { AdminPage } from '@Components/admin/AdminPage'
import {
  getInputNumber,
  NetworkModeItem,
  NetworkModeList,
  showErrorMsg,
  useNetworkModeMap,
} from '@Utils/Shared'
import {
  ChallengeCategoryItem,
  ChallengeCategoryList,
  ChallengeTypeItem,
  useChallengeCategoryLabelMap,
  useChallengeTypeLabelMap,
} from '@Utils/Shared'
import { useEditPool, useEditPools } from '@Hooks/useEdit'
import api, {
  ChallengeCategory,
  ChallengeType,
  Difficulty,
  NetworkMode,
  PoolChallengeUpdateModel,
} from '@Api'
import misc from '@Styles/Misc.module.css'

const PoolChallengeEdit: FC = () => {
  const navigate = useNavigate()
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')

  const { pool, mutate } = useEditPool(numId)
  const { pools, mutate: mutatePools } = useEditPools()

  const challengeCategoryLabelMap = useChallengeCategoryLabelMap()
  const challengeTypeLabelMap = useChallengeTypeLabelMap()
  const networkModeLabelMap = useNetworkModeMap()

  const modals = useModals()
  const { t } = useTranslation()

  const [poolInfo, setPoolInfo] = useState<PoolChallengeUpdateModel>({ ...pool })
  const [deadline, setDeadline] = useState<dayjs.Dayjs | null>(pool?.deadlineUtc ? dayjs(pool?.deadlineUtc) : null)
  const [category, setCategory] = useState<string | null>(pool?.category ?? ChallengeCategory.Misc)
  const [networkMode, setNetworkMode] = useState<string | null>(pool?.networkMode ?? NetworkMode.Open)
  const [difficulty, setDifficulty] = useState<Difficulty | null>(pool?.difficulty ?? Difficulty.Normal)
  const [disabled, setDisabled] = useState(false)

  useEffect(() => {
    if (pool) {
      setPoolInfo({ ...pool, difficulty: pool.difficulty ?? Difficulty.Normal })
      setCategory(pool.category)
      setNetworkMode(pool.networkMode ?? NetworkMode.Open)
      setDifficulty(pool.difficulty ?? Difficulty.Normal)
      setDeadline(pool.deadlineUtc ? dayjs(pool.deadlineUtc) : null)
    }
  }, [pool])

  const difficultyData = Object.values(Difficulty).map((v) => ({
    value: String(v),
    label: t(`exercise.difficulty.${String(v).toLowerCase()}`),
  }))

  const onUpdate = async (model: PoolChallengeUpdateModel, noFeedback?: boolean) => {
    if (!model) return
    setDisabled(true)

    try {
      const res = await api.edit.editUpdatePoolChallenge(numId, {
        ...model,
        deadlineUtc: deadline ? deadline.valueOf() : 0,
      })
      if (!noFeedback) {
        showNotification({
          color: 'teal',
          message: t('admin.notification.pool.updated'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
      }
      mutate(res.data)
      mutatePools()
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      if (!noFeedback) setDisabled(false)
    }
  }

  const onConfirmDelete = async () => {
    setDisabled(true)

    try {
      await api.edit.editRemovePoolChallenge(numId)
      showNotification({
        color: 'teal',
        message: t('admin.notification.pool.deleted'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      mutatePools(
        pools?.filter((p) => p.id !== numId),
        { revalidate: false }
      )
      navigate('/admin/pool')
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setDisabled(false)
    }
  }

  const onCreateTestContainer = async () => {
    try {
      const res = await api.edit.editCreatePoolTestContainer(numId)
      showNotification({
        color: 'teal',
        message: t('admin.notification.games.instances.created'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      if (pool) mutate({ ...pool, testContainer: res.data })
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setDisabled(false)
    }
  }

  const onDestroyTestContainer = async () => {
    try {
      await api.edit.editDestroyPoolTestContainer(numId)
      showNotification({
        color: 'teal',
        message: t('admin.notification.games.instances.deleted'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      if (pool) mutate({ ...pool, testContainer: undefined })
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setDisabled(false)
    }
  }

  const onToggleTestContainer = async () => {
    if (!pool) return
    setDisabled(true)

    await onUpdate({ ...poolInfo, category: category as ChallengeCategory }, true)

    if (pool?.testContainer) {
      await onDestroyTestContainer()
    } else {
      await onCreateTestContainer()
    }
  }

  const type = pool?.type ?? ChallengeType.StaticAttachment
  const isContainer = type === ChallengeType.StaticContainer || type === ChallengeType.DynamicContainer
  const isDynamicContainer = type === ChallengeType.DynamicContainer

  return (
    <AdminPage
      isLoading={!pool}
      headProps={{ justify: 'space-between' }}
      head={
        <Group justify="space-between" w="100%">
          <Group gap="xs" wrap="nowrap">
            <Button
              component={Link}
              to="/admin/pool"
              variant="light"
              leftSection={<Icon path={mdiKeyboardBackspace} size={1} />}
            >
              {t('admin.button.back')}
            </Button>
            <Title order={3} lineClamp={1}>
              {poolInfo.title}
            </Title>
          </Group>
          <Group gap="xs" wrap="nowrap">
            <Button
              disabled={disabled}
              color="red"
              variant="outline"
              leftSection={<Icon path={mdiDeleteOutline} size={1} />}
              onClick={() =>
                modals.openConfirmModal({
                  title: t('admin.button.challenges.delete'),
                  children: <Text size="sm">{t('admin.content.pool.delete', { name: poolInfo.title })}</Text>,
                  onConfirm: () => onConfirmDelete(),
                  confirmProps: { color: 'red' },
                })
              }
            >
              {t('admin.button.challenges.delete')}
            </Button>
            <Button
              component={Link}
              to={`/admin/pool/${numId}/flags`}
              disabled={disabled}
              leftSection={<Icon path={mdiLinkVariant} size={1} />}
            >
              {t('admin.button.challenges.edit_more')}
            </Button>
            <Button
              mr="18px"
              disabled={disabled}
              leftSection={<Icon path={mdiContentSaveOutline} size={1} />}
              onClick={() => onUpdate({ ...poolInfo, category: category as ChallengeCategory })}
            >
              {t('admin.button.save')}
            </Button>
          </Group>
        </Group>
      }
    >
      <Stack>
        <Grid columns={3}>
          <Grid.Col span={1}>
            <TextInput
              label={t('admin.content.games.challenges.title')}
              required
              disabled={disabled}
              value={poolInfo.title ?? ''}
              onChange={(e) => setPoolInfo({ ...poolInfo, title: e.target.value })}
            />
          </Grid.Col>
          <Grid.Col span={1}>
            <Select
              label={t('admin.content.games.challenges.type.label')}
              value={type}
              disabled
              readOnly
              renderOption={ChallengeTypeItem}
              data={Object.entries(ChallengeType).map((type) => {
                const data = challengeTypeLabelMap.get(type[1])
                return { value: type[1], label: data?.name, ...data } as ComboboxItem
              })}
            />
          </Grid.Col>
          <Grid.Col span={1}>
            <Select
              required
              label={t('admin.content.games.challenges.category')}
              value={category}
              disabled={disabled}
              onChange={(e) => {
                setCategory(e)
                setPoolInfo({ ...poolInfo, category: e as ChallengeCategory })
              }}
              renderOption={ChallengeCategoryItem}
              data={ChallengeCategoryList.map((category) => {
                const data = challengeCategoryLabelMap.get(category)
                return { value: category, label: data?.name, ...data } as ComboboxItem
              })}
            />
          </Grid.Col>
          <Grid.Col span={2}>
            <Textarea
              w="100%"
              label={t('admin.content.games.challenges.description')}
              value={poolInfo?.content ?? ''}
              autosize
              disabled={disabled}
              minRows={5}
              maxRows={5}
              onChange={(e) => setPoolInfo({ ...poolInfo, content: e.target.value })}
            />
          </Grid.Col>
          <Grid.Col span={1}>
            <Stack gap="0.425625rem">
              <NumberInput
                label={t('admin.content.games.challenges.submission_limit.label')}
                description={t('admin.content.games.challenges.submission_limit.description')}
                min={0}
                max={10000}
                disabled={disabled}
                value={poolInfo?.submissionLimit ?? 0}
                onChange={(e) => {
                  const number = getInputNumber(e)
                  if (isNaN(number)) return
                  setPoolInfo({ ...poolInfo, submissionLimit: number })
                }}
              />
              <DateTimePicker
                label={t('admin.content.games.challenges.deadline.label')}
                placeholder={t('admin.content.games.challenges.deadline.placeholder')}
                size="sm"
                value={deadline?.toDate()}
                valueFormat="L LT"
                disabled={disabled}
                clearable
                onChange={(e) => setDeadline(e ? dayjs(e) : null)}
              />
            </Stack>
          </Grid.Col>
          <Grid.Col span={1}>
            <HintList
              label={t('admin.content.games.challenges.hints')}
              hints={poolInfo?.hints ?? []}
              disabled={disabled}
              height={180}
              onChangeHint={(hints) => setPoolInfo({ ...poolInfo, hints })}
            />
          </Grid.Col>
          <Grid.Col span={1}>
            <Select
              label={t('admin.content.pool.difficulty')}
              value={String(difficulty)}
              disabled={disabled}
              onChange={(v) => {
                setDifficulty(v as Difficulty | null)
                setPoolInfo({ ...poolInfo, difficulty: v as Difficulty })
              }}
              data={difficultyData}
            />
          </Grid.Col>
          <Grid.Col span={1}>
            <TextInput
              label={t('admin.content.pool.tags')}
              placeholder="web, pwn"
              disabled={disabled}
              value={poolInfo.tags?.join(', ') ?? ''}
              onChange={(e) =>
                setPoolInfo({
                  ...poolInfo,
                  tags: e.target.value
                    .split(',')
                    .map((s) => s.trim())
                    .filter(Boolean),
                })
              }
            />
          </Grid.Col>
        </Grid>

        {/* Range settings */}
        <Group gap="xl" align="flex-start">
          <Switch
            disabled={disabled}
            checked={poolInfo.rangeEnabled ?? false}
            label={SwitchLabel(
              t('admin.content.pool.range.enabled'),
              t('admin.content.pool.range.enabled_description')
            )}
            onChange={(e) => setPoolInfo({ ...poolInfo, rangeEnabled: e.target.checked })}
          />
          <NumberInput
            w="10rem"
            label={t('admin.content.pool.range.score')}
            description={t('admin.content.pool.range.score_description')}
            min={0}
            max={100000}
            disabled={disabled}
            value={poolInfo.rangeScore ?? 500}
            onChange={(e) => {
              const number = getInputNumber(e)
              if (isNaN(number)) return
              setPoolInfo({ ...poolInfo, rangeScore: number })
            }}
          />
          <Switch
            disabled={disabled}
            checked={poolInfo.isEnabled ?? false}
            label={SwitchLabel(t('admin.content.games.challenges.enable'), '')}
            onChange={(e) => setPoolInfo({ ...poolInfo, isEnabled: e.target.checked })}
          />
        </Group>

        {/* Admin note */}
        <Textarea
          label={t('admin.content.pool.note')}
          description={t('admin.content.pool.note_description')}
          value={poolInfo.note ?? ''}
          autosize
          minRows={2}
          maxRows={4}
          disabled={disabled}
          onChange={(e) => setPoolInfo({ ...poolInfo, note: e.target.value })}
        />

        {/* Referenced games */}
        <Stack gap={2}>
          <Text fw="bold" size="sm">
            {t('admin.content.pool.referenced_games')}
          </Text>
          {pool?.referencedGames && pool.referencedGames.length > 0 ? (
            pool.referencedGames.map((ref) => (
              <Group key={ref.gameChallengeId} gap="xs" wrap="nowrap">
                <Icon path={mdiLinkVariant} size={0.8} />
                <Button
                  component={Link}
                  to={`/admin/games/${ref.gameId}/challenges/${ref.gameChallengeId}`}
                  variant="subtle"
                  size="compact-sm"
                >
                  {ref.gameTitle}
                </Button>
              </Group>
            ))
          ) : (
            <Text size="sm" c="dimmed">
              {t('admin.content.pool.no_referenced_games')}
            </Text>
          )}
        </Stack>

        {type === ChallengeType.DynamicAttachment && (
          <TextInput
            label={t('admin.content.games.challenges.attachment_name.label')}
            description={t('admin.content.games.challenges.attachment_name.description')}
            disabled={disabled}
            value={poolInfo.fileName ?? 'attachment'}
            onChange={(e) => setPoolInfo({ ...poolInfo, fileName: e.target.value })}
          />
        )}

        {isDynamicContainer && (
          <TextInput
            label={t('admin.content.games.challenges.flag.template')}
            placeholder="flag{GUID}"
            disabled={disabled}
            value={poolInfo.flagTemplate ?? ''}
            onChange={(e) => setPoolInfo({ ...poolInfo, flagTemplate: e.target.value })}
          />
        )}

        {isContainer && (
          <Grid columns={12}>
            <Grid.Col span={8}>
              <Group justify="space-between" align="flex-end">
                <TextInput
                  label={t('admin.content.games.challenges.container_image')}
                  disabled={disabled}
                  value={poolInfo.containerImage ?? ''}
                  required
                  onChange={(e) => setPoolInfo({ ...poolInfo, containerImage: e.target.value })}
                  classNames={{ root: misc.flexGrow }}
                />
                <NumberInput
                  label={t('admin.content.games.challenges.service_port.label')}
                  min={1}
                  max={65535}
                  w="8rem"
                  required
                  disabled={disabled}
                  value={poolInfo.exposePort ?? 80}
                  onChange={(e) => {
                    const number = getInputNumber(e)
                    if (isNaN(number)) return
                    setPoolInfo({ ...poolInfo, exposePort: number })
                  }}
                />
                <Button miw="8rem" color={pool?.testContainer ? 'orange' : 'green'} disabled={disabled} onClick={onToggleTestContainer}>
                  {pool?.testContainer
                    ? t('admin.button.challenges.test_container.destroy')
                    : t('admin.button.challenges.test_container.create')}
                </Button>
              </Group>
            </Grid.Col>
            <Grid.Col span={4}>
              <InstanceEntry
                test
                label={`${pool?.title} @ ${t('admin.content.pool.title')} (test)`}
                disabled={disabled}
                context={{
                  closeTime: pool?.testContainer?.expectStopAt,
                  instanceEntry: pool?.testContainer?.entry,
                }}
              />
            </Grid.Col>
            <Grid.Col span={2}>
              <Select
                required
                label={t('admin.content.games.challenges.network_mode.label')}
                description={t('admin.content.games.challenges.network_mode.description')}
                value={networkMode ?? NetworkMode.Open}
                disabled={disabled}
                onChange={(e) => {
                  setNetworkMode(e)
                  setPoolInfo({ ...poolInfo, networkMode: e as NetworkMode })
                }}
                renderOption={NetworkModeItem}
                data={NetworkModeList.map((mode) => {
                  const data = networkModeLabelMap.get(mode)
                  return { value: mode, ...data } as ComboboxItem
                })}
              />
            </Grid.Col>
            <Grid.Col span={2}>
              <NumberInput
                label={t('admin.content.games.challenges.cpu_limit.label')}
                min={1}
                max={1024}
                required
                disabled={disabled}
                value={poolInfo.cpuCount ?? 1}
                onChange={(e) => {
                  const number = getInputNumber(e)
                  if (isNaN(number)) return
                  setPoolInfo({ ...poolInfo, cpuCount: number })
                }}
              />
            </Grid.Col>
            <Grid.Col span={2}>
              <NumberInput
                label={t('admin.content.games.challenges.memory_limit.label')}
                min={32}
                max={1048576}
                required
                disabled={disabled}
                value={poolInfo.memoryLimit ?? 32}
                onChange={(e) => {
                  const number = getInputNumber(e)
                  if (isNaN(number)) return
                  setPoolInfo({ ...poolInfo, memoryLimit: number })
                }}
              />
            </Grid.Col>
            <Grid.Col span={2}>
              <NumberInput
                label={t('admin.content.games.challenges.storage_limit.label')}
                min={0}
                max={1048576}
                required
                disabled={disabled}
                value={poolInfo.storageLimit ?? 32}
                onChange={(e) => {
                  const number = getInputNumber(e)
                  if (isNaN(number)) return
                  setPoolInfo({ ...poolInfo, storageLimit: number })
                }}
              />
            </Grid.Col>
          </Grid>
        )}

        {pool?.referencedGames && pool.referencedGames.length > 0 && (
          <Badge color="teal" variant="light">
            {t('admin.content.pool.linked_badge')}
          </Badge>
        )}
      </Stack>
    </AdminPage>
  )
}

export default PoolChallengeEdit
