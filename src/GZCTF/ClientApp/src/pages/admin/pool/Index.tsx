import {
  Badge,
  Button,
  Center,
  ComboboxItem,
  Group,
  Modal,
  NumberInput,
  Pagination,
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
import { mdiArrowDown, mdiArrowUp, mdiCheck, mdiDatabaseOutline, mdiDeleteOutline, mdiPencilOutline, mdiPlus } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router'
import { AdminPage } from '@Components/admin/AdminPage'
import { difficultyIndex, showErrorMsg } from '@Utils/Shared'
import {
  ChallengeCategoryItem,
  ChallengeCategoryList,
  ChallengeTypeItem,
  useChallengeCategoryLabelMap,
  useChallengeTypeLabelMap,
} from '@Utils/Shared'
import { useEditPools } from '@Hooks/useEdit'
import api, { ChallengeCategory, ChallengeType, Difficulty } from '@Api'

const difficultyLabel = (t: (k: string) => string, difficulty: Difficulty | string | null | undefined) =>
  t(`exercise.difficulty.${difficulty?.toLowerCase() ?? 'normal'}`)

type SortKey = 'title' | 'category' | 'type' | 'difficulty' | 'rangeScore' | 'referencedGamesCount'

const SortableTh: FC<{
  label: string
  sortKey: SortKey
  current: SortKey | null
  order: 'asc' | 'desc'
  onSort: (key: SortKey) => void
}> = ({ label, sortKey, current, order, onSort }) => (
  <Table.Th style={{ cursor: 'pointer', userSelect: 'none' }} onClick={() => onSort(sortKey)}>
    <Group gap={4} wrap="nowrap">
      <Text size="sm">{label}</Text>
      {current === sortKey && <Icon path={order === 'asc' ? mdiArrowUp : mdiArrowDown} size={0.7} />}
    </Group>
  </Table.Th>
)

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
  const [difficulty, setDifficulty] = useState<Difficulty | null>(Difficulty.Normal)
  const [disabled, setDisabled] = useState(false)
  const [page, setPage] = useState(1)

  // list filter + sort
  const [typeFilter, setTypeFilter] = useState<ChallengeType | null>(null)
  const [search, setSearch] = useState('')
  const [sortKey, setSortKey] = useState<SortKey | null>(null)
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('asc')

  const onSort = (key: SortKey) => {
    if (sortKey === key) {
      setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc')
    } else {
      setSortKey(key)
      setSortOrder('asc')
    }
  }

  const filteredPools = useMemo(() => {
    let list = (pools ?? []).filter(
      (p) =>
        (!typeFilter || p.type === typeFilter) &&
        (!search || (p.title ?? '').toLowerCase().includes(search.toLowerCase()))
    )
    if (sortKey) {
      list = [...list].sort((a, b) => {
        let cmp = 0
        switch (sortKey) {
          case 'title':
            cmp = (a.title ?? '').localeCompare(b.title ?? '')
            break
          case 'category':
            cmp = (a.category ?? '').localeCompare(b.category ?? '')
            break
          case 'type':
            cmp = (a.type ?? '').localeCompare(b.type ?? '')
            break
          case 'difficulty':
            cmp = difficultyIndex(a.difficulty) - difficultyIndex(b.difficulty)
            break
          case 'rangeScore':
            cmp = (a.rangeScore ?? 0) - (b.rangeScore ?? 0)
            break
          case 'referencedGamesCount':
            cmp = (a.referencedGamesCount ?? 0) - (b.referencedGamesCount ?? 0)
            break
        }
        return sortOrder === 'asc' ? cmp : -cmp
      })
    }
    return list
  }, [pools, typeFilter, search, sortKey, sortOrder])

  // paginate the filtered list client-side (20 per page), newest first from the backend
  const PAGE_SIZE = 20
  const pageCount = Math.max(1, Math.ceil(filteredPools.length / PAGE_SIZE))
  const pagePools = filteredPools.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE)

  // jump back to the first page whenever the filter/sort changes
  useEffect(() => {
    setPage(1)
  }, [typeFilter, search, sortKey, sortOrder])

  const difficultyData = Object.values(Difficulty).map((v) => ({
    value: String(v),
    label: t(`exercise.difficulty.${String(v).toLowerCase()}`),
  }))

  const onCreate = async () => {
    if (!title || !category || !type) return
    setDisabled(true)

    try {
      const res = await api.edit.editAddPoolChallenge({
        title,
        category: category as ChallengeCategory,
        type: type as ChallengeType,
        difficulty: difficulty ?? undefined,
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
    <AdminPage
      isLoading={!pools}
      headProps={{ justify: 'space-between' }}
      head={
        <Group justify="space-between" w="100%" wrap="nowrap">
          <Group gap="md" wrap="nowrap">
            <Title order={2}>{t('admin.content.pool.title')}</Title>
            <TextInput
              placeholder={t('admin.content.games.challenges.title')}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              w="12rem"
            />
            <Select
              placeholder={t('admin.content.show_all')}
              clearable
              searchable
              w="12rem"
              value={typeFilter}
              nothingFoundMessage={t('admin.content.nothing_found')}
              onChange={(value) => setTypeFilter(value as ChallengeType | null)}
              renderOption={ChallengeTypeItem}
              data={Object.entries(ChallengeType).map((type) => {
                const data = challengeTypeLabelMap.get(type[1])
                return { value: type[1], label: data?.name, ...data } as ComboboxItem
              })}
            />
          </Group>
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
      ) : filteredPools.length === 0 ? (
        <Center h="calc(100vh - 200px)">
          <Stack gap={0} align="center">
            <Icon path={mdiDatabaseOutline} size={4} color="gray" />
            <Title order={3}>{t('admin.content.show_all')}</Title>
            <Text>{t('admin.content.nothing_found')}</Text>
          </Stack>
        </Center>
      ) : (
        <>
        <Table highlightOnHover verticalSpacing="sm">
          <Table.Thead>
            <Table.Tr>
              <SortableTh
                label={t('admin.content.games.challenges.title')}
                sortKey="title"
                current={sortKey}
                order={sortOrder}
                onSort={onSort}
              />
              <SortableTh
                label={t('admin.content.games.challenges.category')}
                sortKey="category"
                current={sortKey}
                order={sortOrder}
                onSort={onSort}
              />
              <SortableTh
                label={t('admin.content.games.challenges.type.label')}
                sortKey="type"
                current={sortKey}
                order={sortOrder}
                onSort={onSort}
              />
              <SortableTh
                label={t('admin.content.pool.difficulty')}
                sortKey="difficulty"
                current={sortKey}
                order={sortOrder}
                onSort={onSort}
              />
              <Table.Th>{t('admin.content.pool.range.enabled')}</Table.Th>
              <SortableTh
                label={t('admin.content.pool.range.score')}
                sortKey="rangeScore"
                current={sortKey}
                order={sortOrder}
                onSort={onSort}
              />
              <SortableTh
                label={t('admin.content.pool.referenced_games')}
                sortKey="referencedGamesCount"
                current={sortKey}
                order={sortOrder}
                onSort={onSort}
              />
              <Table.Th />
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {pagePools.map((pool) => {
              const cateData = challengeCategoryLabelMap.get(pool.category ?? ChallengeCategory.Misc)
              const typeData = challengeTypeLabelMap.get(pool.type!)
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
                      onChange={() => onToggleRange(pool.id!, pool.rangeEnabled ?? false)}
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
        <Group justify="space-between" align="center" mt="md" px="xs">
          <Text size="sm" c="dimmed">
            {t('admin.content.pool.page_info', {
              current: Math.min((page - 1) * PAGE_SIZE + pagePools.length, filteredPools.length),
              total: filteredPools.length,
            })}
          </Text>
          <Pagination.Root total={pageCount} siblings={1} value={page} onChange={setPage}>
            <Group gap={5} justify="flex-end">
              <Pagination.First />
              <Pagination.Previous />
              <Pagination.Items />
              <Pagination.Next />
              <Pagination.Last />
            </Group>
          </Pagination.Root>
        </Group>
        </>
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
            onChange={(v) => setDifficulty(v as Difficulty | null)}
            data={difficultyData}
          />
          <Button fullWidth disabled={disabled} onClick={onCreate}>
            {t('admin.button.pool.new')}
          </Button>
        </Stack>
      </Modal>
    </AdminPage>
  )
}

export default PoolIndex
