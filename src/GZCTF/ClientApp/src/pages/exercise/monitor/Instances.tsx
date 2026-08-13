import {
  Badge,
  Code,
  Group,
  Paper,
  ScrollArea,
  Table,
  Text,
  Tooltip,
  useMantineTheme,
} from '@mantine/core'
import { useClipboard } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiPackageVariantClosedRemove, mdiPuzzleOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { ActionIconWithConfirm } from '@Components/ActionIconWithConfirm'
import { WithExerciseMonitor } from '@Components/WithExerciseMonitor'
import { useLanguage } from '@Utils/I18n'
import { showErrorMsg } from '@Utils/Shared'
import api from '@Api'
import tableClasses from '@Styles/Table.module.css'

const Instances: FC = () => {
  const { data } = api.admin.useAdminExerciseInstances({ refreshInterval: 10 * 1000 })
  const instances = data?.data

  const [disabled, setDisabled] = useState(false)

  const { t } = useTranslation()
  const { locale } = useLanguage()
  const theme = useMantineTheme()
  const { copy, copied } = useClipboard()

  const onDelete = async (containerGuid?: string) => {
    if (!containerGuid) return

    try {
      setDisabled(true)
      await api.admin.adminDestroyExerciseInstance(containerGuid)

      showNotification({
        color: 'teal',
        message: t('admin.notification.instances.destroyed'),
        icon: <Icon path={mdiCheck} size={1} />,
      })

      await api.admin.mutateAdminExerciseInstances()
    } catch (e: any) {
      showErrorMsg(e, t)
    } finally {
      setDisabled(false)
    }
  }

  return (
    <WithExerciseMonitor isLoading={!instances}>
      <Paper shadow="md" p="md">
        <ScrollArea offsetScrollbars h="calc(100vh - 200px)">
          <Table className={tableClasses.table}>
            <Table.Thead>
              <Table.Tr>
                <Table.Th>{t('common.label.user')}</Table.Th>
                <Table.Th>{t('common.label.challenge')}</Table.Th>
                <Table.Th>{t('admin.label.instances.image')}</Table.Th>
                <Table.Th>{t('admin.label.instances.life_cycle')}</Table.Th>
                <Table.Th>{t('admin.label.instances.entry')}</Table.Th>
                <Table.Th />
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {instances?.map((instance) => (
                <Table.Tr key={instance.containerGuid}>
                  <Table.Td>
                    <Text ff="monospace" size="sm" fw="bold">
                      {instance.user?.userName ?? 'User'}
                    </Text>
                  </Table.Td>
                  <Table.Td>
                    <Group gap={4} wrap="nowrap">
                      <Icon path={mdiPuzzleOutline} size={0.8} />
                      <Text size="sm">{instance.challenge?.title ?? 'Challenge'}</Text>
                    </Group>
                  </Table.Td>
                  <Table.Td>
                    <Code>{instance.image}</Code>
                  </Table.Td>
                  <Table.Td ff="monospace">
                    <Group gap={4} wrap="nowrap">
                      <Badge size="sm" color="indigo">
                        {dayjs(instance.startedAt).locale(locale).format('MM-DD HH:mm')}
                      </Badge>
                      <Text size="xs" c="dimmed">
                        ~
                      </Text>
                      <Badge size="sm" color="gray">
                        {dayjs(instance.expectStopAt).locale(locale).format('MM-DD HH:mm')}
                      </Badge>
                    </Group>
                  </Table.Td>
                  <Table.Td>
                    <Tooltip label={t('admin.content.instances.copy_hint')}>
                      <Code
                        style={{ cursor: 'pointer' }}
                        onClick={() => copy(`${instance.ip}:${instance.port}`)}
                      >
                        {instance.ip}:{instance.port}
                      </Code>
                    </Tooltip>
                  </Table.Td>
                  <Table.Td>
                    <ActionIconWithConfirm
                      iconPath={mdiPackageVariantClosedRemove}
                      color="red"
                      message={t('admin.content.instances.destroy')}
                      disabled={disabled}
                      onClick={() => onDelete(instance.containerGuid)}
                    />
                  </Table.Td>
                </Table.Tr>
              ))}
            </Table.Tbody>
          </Table>
        </ScrollArea>
      </Paper>
    </WithExerciseMonitor>
  )
}

export default Instances
