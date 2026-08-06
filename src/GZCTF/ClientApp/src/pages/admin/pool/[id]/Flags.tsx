import {
  ActionIcon,
  Button,
  Center,
  Chip,
  Divider,
  FileButton,
  Group,
  Input,
  Modal,
  Overlay,
  Progress,
  ScrollArea,
  Stack,
  Text,
  Textarea,
  TextInput,
  Title,
  alpha,
  useMantineColorScheme,
  useMantineTheme,
} from '@mantine/core'
import { useModals } from '@mantine/modals'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose, mdiKeyboardBackspace } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router'
import { FlagEditPanel } from '@Components/admin/FlagEditPanel'
import { AdminPage } from '@Components/admin/AdminPage'
import { showErrorMsg } from '@Utils/Shared'
import { useDisplayInputStyles } from '@Utils/ThemeOverride'
import { useEditPool } from '@Hooks/useEdit'
import api, { ChallengeType, FileType, FlagCreateModel, FlagInfoModel } from '@Api'
import misc from '@Styles/Misc.module.css'
import uploadClasses from '@Styles/Upload.module.css'

const PoolFlags: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')

  const modals = useModals()
  const { colorScheme } = useMantineColorScheme()
  const theme = useMantineTheme()
  const { classes } = useDisplayInputStyles({ fw: 'bold', ff: 'monospace' })
  const { t } = useTranslation()

  const { pool, mutate } = useEditPool(numId)

  const [disabled, setDisabled] = useState(false)
  const [type, setType] = useState<FileType>(pool?.attachment?.type ?? FileType.None)
  const [remoteUrl, setRemoteUrl] = useState(pool?.attachment?.url ?? '')
  const [progress, setProgress] = useState(0)
  const [flagCreateModalOpen, setFlagCreateModalOpen] = useState(false)
  const [newFlag, setNewFlag] = useState('')
  const [uploadModalOpened, setUploadModalOpened] = useState(false)
  const [remoteModalOpened, setRemoteModalOpened] = useState(false)
  const [files, setFiles] = useState<File[]>([])
  const [remoteText, setRemoteText] = useState('')

  const FileTypeDesrcMap = new Map<FileType, string>([
    [FileType.None, t('challenge.file_type.none')],
    [FileType.Remote, t('challenge.file_type.remote')],
    [FileType.Local, t('challenge.file_type.local')],
  ])

  useEffect(() => {
    if (pool) {
      setType(pool.attachment?.type ?? FileType.None)
      setRemoteUrl(pool.attachment?.url ?? '')
    }
  }, [pool])

  const onConfirmClear = async () => {
    setDisabled(true)

    try {
      await api.edit.editUpdatePoolAttachment(numId, { attachmentType: FileType.None })
      showNotification({
        color: 'teal',
        message: t('admin.notification.games.challenges.attachment.updated'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      setType(FileType.None)
      if (pool) mutate({ ...pool, attachment: undefined })
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setDisabled(false)
    }
  }

  const onUpload = async (file: File | null) => {
    if (!file) return

    setProgress(0)
    setDisabled(true)

    try {
      const res = await api.assets.assetsUpload(
        { files: [file] },
        undefined,
        {
          onUploadProgress: (e) => {
            setProgress((e.loaded / (e.total ?? 1)) * 90)
          },
        }
      )
      const remoteFile = res.data[0]
      setProgress(95)
      if (remoteFile) {
        await api.edit.editUpdatePoolAttachment(numId, {
          attachmentType: FileType.Local,
          fileHash: remoteFile.hash,
        })
        setProgress(0)
        mutate()
        showNotification({
          color: 'teal',
          message: t('admin.notification.games.challenges.attachment.updated'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
      }
    } catch (err) {
      showErrorMsg(err, t)
    } finally {
      setDisabled(false)
    }
  }

  const onRemote = async () => {
    if (!remoteUrl.startsWith('http')) return
    setDisabled(true)

    try {
      await api.edit.editUpdatePoolAttachment(numId, {
        attachmentType: FileType.Remote,
        remoteUrl,
      })
      showNotification({
        color: 'teal',
        message: t('admin.notification.games.challenges.attachment.updated'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setDisabled(false)
    }
  }

  const onConfirmAddFlag = async () => {
    const flags = newFlag
      .split('\n')
      .map((f) => f.trim())
      .filter(Boolean)
    if (flags.length === 0) return

    setDisabled(true)

    try {
      await api.edit.editAddPoolFlags(numId, flags.map((flag) => ({ flag })))
      showNotification({
        color: 'teal',
        message: t('admin.notification.games.challenges.flag.created'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      setNewFlag('')
      setFlagCreateModalOpen(false)
      mutate()
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setDisabled(false)
    }
  }

  const onDeleteFlag = (flag: FlagInfoModel) => {
    modals.openConfirmModal({
      title: t('admin.button.challenges.flag.delete'),
      size: '35%',
      children: (
        <Stack>
          <Text>{t('admin.content.games.challenges.flag.delete')}</Text>
          <Input variant="unstyled" value={flag.flag} w="100%" size="md" readOnly classNames={classes} />
        </Stack>
      ),
      onConfirm: () => flag.id && onConfirmDeleteFlag(flag.id),
      confirmProps: { color: 'red' },
    })
  }

  const onConfirmDeleteFlag = async (flagId: number) => {
    try {
      await api.edit.editRemovePoolFlag(numId, flagId)
      showNotification({
        color: 'teal',
        message: t('admin.notification.games.challenges.flag.deleted'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      if (pool) mutate({ ...pool, flags: pool.flags.filter((f) => f.id !== flagId) })
    } catch (e) {
      showErrorMsg(e, t)
    }
  }

  // DynamicAttachment: batch upload one flag per file (flag = file name)
  const onUploadAttachments = async () => {
    if (files.length === 0) return

    setProgress(0)
    setDisabled(true)

    try {
      const res = await api.assets.assetsUpload(
        { files },
        { filename: pool?.fileName ?? 'attachment' },
        {
          onUploadProgress: (e) => {
            setProgress((e.loaded / (e.total ?? 1)) * 90)
          },
        }
      )
      setProgress(95)
      if (res.data) {
        await api.edit.editAddPoolFlags(
          numId,
          res.data.map((f, idx) => ({
            flag: files[idx].name,
            attachmentType: FileType.Local,
            fileHash: f.hash,
          }))
        )
        setProgress(0)
        setFiles([])
        setUploadModalOpened(false)
        mutate()
        showNotification({
          color: 'teal',
          message: t('admin.notification.games.challenges.attachment.updated'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
      }
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setDisabled(false)
    }
  }

  // DynamicAttachment: add remote attachments with flags (one "flag url" pair per line)
  const onUploadRemote = async () => {
    const flags: FlagCreateModel[] = []
    remoteText.split('\n').forEach((line) => {
      let part = line.split(' ')
      part = part.length === 1 ? line.split('\t') : part
      if (part.length !== 2) return
      flags.push({ flag: part[0], attachmentType: FileType.Remote, remoteUrl: part[1] })
    })
    if (flags.length === 0) return

    setDisabled(true)

    try {
      await api.edit.editAddPoolFlags(numId, flags)
      showNotification({
        color: 'teal',
        message: t('admin.notification.games.challenges.attachment.updated'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      setRemoteText('')
      setRemoteModalOpened(false)
      mutate()
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setDisabled(false)
    }
  }

  return (
    <AdminPage
      isLoading={!pool}
      headProps={{ justify: 'space-between' }}
      head={
        <Group justify="space-between" w="100%">
          <Button component={Link} to={`/admin/pool/${numId}`} variant="light" leftSection={<Icon path={mdiKeyboardBackspace} size={1} />}>
            {t('admin.button.back')}
          </Button>
          <Title order={3}># {pool?.title}</Title>
        </Group>
      }
    >
      <Stack px="md">
        {/* Attachment */}
        <Group justify="space-between" wrap="nowrap" mt="md">
          <Title order={2}>{t('admin.content.games.challenges.attachment.title')}</Title>
          {type !== FileType.Remote ? (
            <FileButton onChange={onUpload}>
              {(props) => (
                <Button
                  {...props}
                  fullWidth
                  className={uploadClasses.button}
                  disabled={type !== FileType.Local}
                  w="122px"
                  color={progress !== 0 ? 'cyan' : theme.primaryColor}
                >
                  <div className={uploadClasses.label}>
                    {progress !== 0
                      ? t('admin.button.challenges.attachment.uploading')
                      : t('admin.button.challenges.attachment.upload')}
                  </div>
                  {progress !== 0 && (
                    <Progress
                      value={progress}
                      className={uploadClasses.progress}
                      color={alpha(theme.colors[theme.primaryColor][2], 0.35)}
                      radius="sm"
                    />
                  )}
                </Button>
              )}
            </FileButton>
          ) : (
            <Button disabled={disabled} w="122px" onClick={onRemote}>
              {t('admin.button.challenges.attachment.save_url')}
            </Button>
          )}
        </Group>
        <Divider />
        <Group justify="space-between" wrap="nowrap">
          <Input.Wrapper label={t('admin.content.games.challenges.attachment.type')} required>
            <Chip.Group
              value={type}
              onChange={(e) => {
                if (e === FileType.None) {
                  modals.openConfirmModal({
                    title: t('admin.content.games.challenges.attachment.clear.title'),
                    children: (
                      <Text size="sm">{t('admin.content.games.challenges.attachment.clear.description')}</Text>
                    ),
                    onConfirm: onConfirmClear,
                    confirmProps: { color: 'orange' },
                  })
                } else {
                  setType(e as FileType)
                }
              }}
            >
              <Group justify="left" gap="sm" h="2.25rem" wrap="nowrap">
                {Object.entries(FileType).map((type) => (
                  <Chip key={type[0]} value={type[1]} size="sm">
                    {FileTypeDesrcMap.get(type[1])}
                  </Chip>
                ))}
              </Group>
            </Chip.Group>
          </Input.Wrapper>
          {type !== FileType.Remote ? (
            <TextInput
              label={t('admin.content.games.challenges.attachment.link')}
              readOnly
              disabled={disabled || type === FileType.None}
              value={pool?.attachment?.url ?? ''}
              w="calc(100% - 400px)"
              classNames={{ input: uploadClasses.hover }}
              onClick={() => pool?.attachment?.url && window.open(pool?.attachment?.url, '_blank')}
            />
          ) : (
            <TextInput
              label={t('admin.content.games.challenges.attachment.link')}
              disabled={disabled}
              value={remoteUrl}
              w="calc(100% - 400px)"
              classNames={{ input: uploadClasses.hover }}
              onChange={(e) => setRemoteUrl(e.target.value)}
            />
          )}
        </Group>

        {/* Flags */}
        <Group justify="space-between" mt="md">
          <Title order={2}>{t('admin.content.games.challenges.flag.title')}</Title>
          {pool?.type === ChallengeType.DynamicAttachment ? (
            <Group justify="right" gap="xs">
              <Button disabled={disabled} w="122px" onClick={() => setRemoteModalOpened(true)}>
                {t('admin.button.challenges.flag.add.remote')}
              </Button>
              <Button disabled={disabled} w="122px" onClick={() => setUploadModalOpened(true)}>
                {t('admin.button.challenges.flag.add.dynamic')}
              </Button>
            </Group>
          ) : (
            <Button disabled={disabled} w="122px" onClick={() => setFlagCreateModalOpen(true)}>
              {t('admin.button.challenges.flag.add.normal')}
            </Button>
          )}
        </Group>
        <Divider />
        <ScrollArea h="calc(100vh - 30rem)" pos="relative">
          {!pool?.flags.length && (
            <>
              <Overlay opacity={0.3} color={colorScheme === 'dark' ? 'black' : 'white'} />
              <Center h="calc(100vh - 30rem)">
                <Stack gap={0}>
                  <Title order={2}>{t('admin.content.games.challenges.flag.empty.title')}</Title>
                  <Text>{t('admin.content.games.challenges.flag.empty.description')}</Text>
                </Stack>
              </Center>
            </>
          )}
          <FlagEditPanel flags={pool?.flags} onDelete={onDeleteFlag} unifiedAttachment={pool?.attachment} />
        </ScrollArea>
      </Stack>

      <Modal opened={flagCreateModalOpen} onClose={() => setFlagCreateModalOpen(false)} title={t('admin.button.challenges.flag.add.normal')} size="35%">
        <Stack>
          <Textarea
            label={t('admin.content.games.challenges.flag.create')}
            value={newFlag}
            autosize
            minRows={3}
            onChange={(e) => setNewFlag(e.target.value)}
          />
          <Button fullWidth disabled={disabled} onClick={onConfirmAddFlag}>
            {t('admin.button.challenges.flag.add.normal')}
          </Button>
        </Stack>
      </Modal>

      {/* DynamicAttachment: batch upload, one flag per file */}
      <Modal
        opened={uploadModalOpened}
        onClose={() => setUploadModalOpened(false)}
        title={t('admin.button.challenges.flag.add.dynamic')}
        size="40%"
      >
        <Stack>
          <Text size="sm">
            {t('admin.content.games.challenges.attachment.instruction.dynamic.content')}
            <br />
            <Text fw="bold" span>
              {t('admin.content.games.challenges.attachment.instruction.dynamic.format')}
            </Text>
            <br />
          </Text>
          <ScrollArea offsetScrollbars h="30vh" pos="relative">
            {files.length === 0 ? (
              <>
                <Overlay opacity={0.3} color={colorScheme === 'dark' ? 'black' : 'white'} />
                <Center h="calc(30vh - 20px)">
                  <Text>{t('admin.placeholder.games.challenges.attachment.no_file_selected.title')}</Text>
                </Center>
              </>
            ) : (
              <Stack gap="xs">
                {files.map((file) => (
                  <Group key={file.name} justify="space-between" wrap="nowrap">
                    <Text lineClamp={1} ff="monospace">
                      {file.name}
                    </Text>
                    <ActionIcon onClick={() => setFiles(files.filter((f) => f !== file))}>
                      <Icon path={mdiClose} size={1} />
                    </ActionIcon>
                  </Group>
                ))}
              </Stack>
            )}
          </ScrollArea>
          <Group grow>
            <FileButton multiple onChange={setFiles}>
              {(props) => (
                <Button {...props} disabled={disabled}>
                  {t('common.button.select_file')}
                </Button>
              )}
            </FileButton>
            <Button disabled={disabled || files.length < 1} onClick={onUploadAttachments}>
              {t('admin.button.challenges.flag.add.dynamic')}
            </Button>
          </Group>
        </Stack>
      </Modal>

      {/* DynamicAttachment: remote attachments with flags */}
      <Modal
        opened={remoteModalOpened}
        onClose={() => setRemoteModalOpened(false)}
        title={t('admin.button.challenges.flag.add.remote')}
        size="35%"
      >
        <Stack>
          <Text size="sm">
            {t('admin.content.games.challenges.attachment.instruction.remote.content')}
            <br />
            <Text fw="bold" span>
              {t('admin.content.games.challenges.attachment.instruction.remote.format')}
            </Text>
          </Text>
          <Textarea
            required
            autosize
            minRows={8}
            maxRows={12}
            value={remoteText}
            classNames={{ input: misc.ffmono }}
            onChange={(e) => setRemoteText(e.target.value)}
            placeholder={'flag{hello_world} http://example.com/1.zip'}
          />
          <Button fullWidth disabled={disabled} onClick={onUploadRemote}>
            {t('admin.button.games.challenges.attachment.batch_add')}
          </Button>
        </Stack>
      </Modal>
    </AdminPage>
  )
}

export default PoolFlags
