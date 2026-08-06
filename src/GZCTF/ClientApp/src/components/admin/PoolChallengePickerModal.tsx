import {
  Button,
  ComboboxItem,
  Group,
  Modal,
  ModalProps,
  NumberInput,
  Select,
  Slider,
  Stack,
  Switch,
  Text,
  TextInput,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiDatabaseOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { showErrorMsg } from '@Utils/Shared'
import { ChallengeCategoryItem, ChallengeCategoryList, useChallengeCategoryLabelMap } from '@Utils/Shared'
import { useEditPools } from '@Hooks/useEdit'
import api, { ChallengeCategory, PoolChallengeInfoModel } from '@Api'

interface PoolChallengePickerModalProps extends ModalProps {
  onAdded: () => void
}

export const PoolChallengePickerModal: FC<PoolChallengePickerModalProps> = (props) => {
  const { id } = useParams()
  const { onAdded, ...modalProps } = props
  const numId = parseInt(id ?? '-1')

  const { pools } = useEditPools()
  const challengeCategoryLabelMap = useChallengeCategoryLabelMap()
  const { t } = useTranslation()

  const [search, setSearch] = useState('')
  const [category, setCategory] = useState<ChallengeCategory | null>(null)
  const [selected, setSelected] = useState<PoolChallengeInfoModel | null>(null)
  const [originalScore, setOriginalScore] = useState<number | undefined>(1000)
  const [minRate, setMinRate] = useState(25)
  const [difficulty, setDifficulty] = useState<number | undefined>(5)
  const [disableBloodBonus, setDisableBloodBonus] = useState(false)
  const [enableTrafficCapture, setEnableTrafficCapture] = useState(false)
  const [disabled, setDisabled] = useState(false)

  const filtered = (pools ?? []).filter(
    (p) =>
      (!category || p.category === category) && (!search || p.title?.toLowerCase().includes(search.toLowerCase()))
  )

  const onLink = async () => {
    if (!selected?.id) return
    setDisabled(true)

    try {
      await api.edit.editAddGameChallengeFromPool(numId, {
        poolChallengeId: selected.id,
        originalScore,
        minScoreRate: minRate / 100,
        difficulty,
        disableBloodBonus,
        enableTrafficCapture,
      })
      showNotification({
        color: 'teal',
        message: t('admin.notification.games.challenges.created'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      onAdded()
      props.onClose()
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setDisabled(false)
    }
  }

  return (
    <Modal {...modalProps}>
      <Stack>
        <Group gap="sm" align="flex-end" wrap="nowrap">
          <TextInput
            label={t('admin.content.show_all')}
            placeholder="Search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            style={{ flex: 1 }}
          />
          <Select
            clearable
            w="12rem"
            placeholder={t('admin.content.show_all')}
            value={category}
            onChange={(v) => setCategory(v as ChallengeCategory | null)}
            renderOption={ChallengeCategoryItem}
            data={ChallengeCategoryList.map((cate) => {
              const data = challengeCategoryLabelMap.get(cate)
              return { value: cate, label: data?.name, ...data } as ComboboxItem
            })}
          />
        </Group>
        <Select
          searchable
          required
          placeholder="Search challenges"
          value={selected ? String(selected.id) : null}
          onChange={(v) => setSelected(filtered.find((p) => String(p.id) === v) ?? null)}
          data={filtered.map((p) => ({ value: String(p.id), label: `${p.title} (${p.rangeScore}pts)` }))}
          nothingFoundMessage={t('admin.content.nothing_found')}
        />
        {selected && (
          <>
            <Group grow>
              <NumberInput
                label={t('admin.content.games.challenges.score')}
                min={0}
                value={originalScore}
                onChange={(v) => setOriginalScore(v as number)}
              />
              <NumberInput
                label={t('admin.content.games.challenges.difficulty')}
                min={0.1}
                step={0.2}
                value={difficulty}
                onChange={(v) => setDifficulty(v as number)}
              />
            </Group>
            <Slider
              label={(v) =>
                t('admin.content.games.challenges.min_score_radio.description', {
                  min_score: ((v / 100) * (originalScore ?? 1000)).toFixed(0),
                })
              }
              value={minRate}
              marks={[
                { value: 20, label: '20%' },
                { value: 50, label: '50%' },
                { value: 80, label: '80%' },
              ]}
              onChange={setMinRate}
            />
            <Switch
              checked={!disableBloodBonus}
              label={t('admin.content.games.challenges.blood_bonus.label')}
              onChange={(e) => setDisableBloodBonus(!e.target.checked)}
            />
            <Switch
              checked={enableTrafficCapture}
              label={t('admin.content.games.challenges.traffic_capture.label')}
              onChange={(e) => setEnableTrafficCapture(e.target.checked)}
            />
          </>
        )}
        <Button fullWidth disabled={disabled || !selected} onClick={onLink}>
          {t('admin.button.pool.link_to_game')}
        </Button>
      </Stack>
    </Modal>
  )
}
