import {
  Badge,
  Button,
  Center,
  ComboboxItem,
  Group,
  Modal,
  NumberInput,
  Select,
  Stack,
  Switch,
  Table,
  Text,
  TextInput,
  Title,
} from '@mantine/core'
import { useInputState } from '@mantine/hooks'
import { useModals } from '@mantine/modals'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiDatabaseOutline, mdiDeleteOutline, mdiPencilOutline, mdiPlus } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router'
import { WithAdminTab } from '@Components/admin/WithAdminTab'
import { showErrorMsg } from '@Utils/Shared'
import {
  ChallengeCategoryItem,
  ChallengeCategoryList,
  ChallengeTypeItem,
  useChallengeCategoryLabelMap,
  useChallengeTypeLabelMap,
} from '@Utils/Shared'
import { useEditPools } from '@Hooks/useEdit'
import api, { ChallengeCategory, ChallengeType, Difficulty } from '@Api'

const difficultyLabel = (t: (k: string) => string, difficulty: Difficulty) =>
  t(`exercise.difficulty.${Difficulty[difficulty ?? Difficulty.Normal]?.toLowerCase() ?? 'normal'}`)

const PoolIndex: FC = () => {
  const navigate = useNavigate()
  const { t } = useTranslation()
  const modals = useModals()

  const { pools, mutate } = useEditPools()
  const challengeCategoryLabelMap = useChallengeCategoryLabelMap()
  const challengeTypeLabelMap = useChallengeTypeLabelMap()

  const [createOpened, setCreateOpened] = useState(false)
  const [title, setTitle] = useInputState('')
  const [category, setCategory] = useState<string | null>(null)
  const [type, setType] = useState<string | null>(null)
  const [difficulty, setDifficulty] = useState<number>(Difficulty.Normal)
  const [disabled, setDisabled] = useState(false)

  const difficultyData = Object.values(Difficulty)
    .filter((v) => typeof v === 'number')
    .map((v) => ({ value: String(v), label: t(`exercise.difficulty.${Difficulty[v as number].toLowerCase()}`) }))

  const onCreate = async () => {
    if (!title || !category || !type) return
    setDisabled(true)

    try {
      const res = await api.edit.editAddPoolChallenge({
        title,
        category: category as ChallengeCategory,
        type: type as ChallengeType,
        difficulty,
      })
      showNotification({
        color: 'teal',
        message: t('admin.notification.pool.created'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      setCreateOpened(false)
      mutate()
      navigate(`/admin/pool/${res.data.id}`)
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setDisabled(false)
    }
  }

  const onToggleRange = async (id: number, rangeEnabled: boolean) => {
    try {
      const res = await api.edit.editUpdatePoolChallenge(id, { rangeEnabled: !rangeEnabled })
      showNotification({
        color: 'teal',
        message: t('admin.notification.pool.range_updated'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      mutate(pools?.map((p) => (p.id === id ? { ...p, rangeEnabled: res.data.rangeEnabled, rangeScore: res.data.rangeScore } : p)))
    } catch (e) {
      showErrorMsg(e, t)
    }
  }

  const onDelete = (id: number, name: string) => {
    modals.openConfirmModal({
      title: t('admin.button.challenges.delete'),
      children: <Text size="sm">{t('admin.content.pool.delete', { name })}</Text>,
      onConfirm: async () => {
        try {
          await api.edit.editRemovePoolChallenge(id)
          showNotification({
            color: 'teal',
            message: t('admin.notification.pool.deleted'),
            icon: <Icon path={mdiCheck} size={1} />,
          })
          mutate(pools?.filter((p) => p.id !== id))
        } catch (e) {
          showErrorMsg(e, t)
        }
      },
      confirmProps: { color: 'red' },
    })
  }

  return (
    <WithAdminTab
      isLoading={!pools}
      headProps={{ justify: 'space-between' }}
      head={
        <Group justify="space-between" w="100%">
          <Title order={2}>{t('admin.content.pool.title')}</Title>
          <Button mr="18px" leftSection={<Icon path={mdiPlus} size={1} />} onClick={() => setCreateOpened(true)}>
            {t('admin.button.pool.new')}
          </Button>
        </Group>
      }
    >
      {!pools || pools.length === 0 ? (
        <Center h="calc(100vh - 200px)">
          <Stack gap={0} align="center">
            <Icon path={mdiDatabaseOutline} size={4} color="gray" />
            <Title order={2}>{t('admin.content.pool.empty.title')}</Title>
            <Text>{t('admin.content.pool.empty.description')}</Text>
          </Stack>
        </Center>
      ) : (
        <Table highlightOnHover verticalSpacing="sm">
          <Table.Thead>
            <Table.Tr>
              <Table.Th>{t('admin.content.games.challenges.title')}</Table.Th>
              <Table.Th>{t('admin.content.games.challenges.category')}</Table.Th>
              <Table.Th>{t('admin.content.games.challenges.type.label')}</Table.Th>
              <Table.Th>{t('admin.content.pool.difficulty')}</Table.Th>
              <Table.Th>{t('admin.content.pool.range.enabled')}</Table.Th>
              <Table.Th>{t('admin.content.pool.range.score')}</Table.Th>
              <Table.Th>{t('admin.content.pool.referenced_games')}</Table.Th>
              <Table.Th />
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {pools.map((pool) => {
              const cateData = challengeCategoryLabelMap.get(pool.category)
              const typeData = challengeTypeLabelMap.get(pool.type)
              return (
                <Table.Tr key={pool.id}>
                  <Table.Td>
                    <Group gap="xs" wrap="nowrap">
                      <Text fw="bold">{pool.title}</Text>
                      {pool.rangeEnabled && (
                        <Badge color="teal" size="xs">
                          {t('admin.content.pool.linked_badge')}
                        </Badge>
                      )}
                    </Group>
                  </Table.Td>
                  <Table.Td>
                    <Group gap="xs" wrap="nowrap">
                      <Icon path={cateData?.icon ?? mdiDatabaseOutline} size={0.9} color={cateData?.color} />
                      <Text size="sm">{cateData?.name}</Text>
                    </Group>
                  </Table.Td>
                  <Table.Td>
                    <Text size="sm">{typeData?.name}</Text>
                  </Table.Td>
                  <Table.Td>
                    <Text size="sm">{difficultyLabel(t, pool.difficulty)}</Text>
                  </Table.Td>
                  <Table.Td>
                    <Switch
                      checked={pool.rangeEnabled}
                      onChange={() => onToggleRange(pool.id!, pool.rangeEnabled)}
                    />
                  </Table.Td>
                  <Table.Td>
                    <Text fw="bold" ff="monospace">
                      {pool.rangeScore}
                    </Text>
                  </Table.Td>
                  <Table.Td>{pool.referencedGamesCount}</Table.Td>
                  <Table.Td>
                    <Group gap="xs" justify="right" wrap="nowrap">
                      <Button
                        size="compact-sm"
                        variant="light"
                        component={Link}
                        to={`/admin/pool/${pool.id}`}
                        leftSection={<Icon path={mdiPencilOutline} size={0.9} />}
                      >
                        {t('admin.button.challenges.edit')}
                      </Button>
                      <Button
                        size="compact-sm"
                        color="red"
                        variant="outline"
                        leftSection={<Icon path={mdiDeleteOutline} size={0.9} />}
                        onClick={() => onDelete(pool.id!, pool.title)}
                      >
                        {t('admin.button.challenges.delete')}
                      </Button>
                    </Group>
                  </Table.Td>
                </Table.Tr>
              )
            })}
          </Table.Tbody>
        </Table>
      )}

      <Modal opened={createOpened} onClose={() => setCreateOpened(false)} title={t('admin.button.pool.new')} size="30%">
        <Stack>
          <TextInput
            label={t('admin.content.games.challenges.title')}
            required
            placeholder="Title"
            value={title}
            onChange={setTitle}
          />
          <Select
            required
            label={t('admin.content.games.challenges.category')}
            placeholder="Category"
            value={category}
            onChange={setCategory}
            renderOption={ChallengeCategoryItem}
            data={ChallengeCategoryList.map((category) => {
              const data = challengeCategoryLabelMap.get(category)
              return { value: category, label: data?.name, ...data } as ComboboxItem
            })}
          />
          <Select
            required
            label={t('admin.content.games.challenges.type.label')}
            description={t('admin.content.games.challenges.type.description')}
            placeholder="Type"
            value={type}
            onChange={setType}
            renderOption={ChallengeTypeItem}
            data={Object.entries(ChallengeType).map((type) => {
              const data = challengeTypeLabelMap.get(type[1])
              return { value: type[1], label: data?.name, ...data } as ComboboxItem
            })}
          />
          <Select
            label={t('admin.content.pool.difficulty')}
            value={String(difficulty)}
            onChange={(v) => setDifficulty(Number(v))}
            data={difficultyData}
          />
          <Button fullWidth disabled={disabled} onClick={onCreate}>
            {t('admin.button.pool.new')}
          </Button>
        </Stack>
      </Modal>
    </WithAdminTab>
  )
}

export default PoolIndex
