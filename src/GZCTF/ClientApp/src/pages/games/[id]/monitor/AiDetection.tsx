import { Alert, Badge, Button, Group, Paper, Stack, Table, Text, Title } from '@mantine/core'
import dayjs from 'dayjs'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import useSWR from 'swr'
import { WithGameMonitor } from '@Components/WithGameMonitor'

interface AiDetectionInfo {
  id: string
  challengeId: number
  challengeTitle: string
  teamId: number
  teamName: string
  participationId: number
  modelId: string
  seenAt: string
  markedFlagIssued?: boolean
  markedFlagSubmitted?: boolean
  markedFlagIssuedAt?: string | null
  markedFlagSubmittedAt?: string | null
  markedFlagValue?: string | null
}

interface AiMarkedFlagInfo {
  id: string
  challengeId: number
  challengeTitle: string
  teamId: number
  teamName: string
  participationId: number
  modelId: string
  flag: string
  issuedAtUtc: string
  submittedAtUtc?: string | null
  submissionId?: number | null
  submittedAnswer?: string | null
  submissionTime?: string | null
}

const AiDetection: FC = () => {
  const { id } = useParams()
  const { t, i18n } = useTranslation()
  const isChinese = i18n.language.startsWith('zh')
  const { data, error } = useSWR<AiDetectionInfo[]>(
    id ? `/api/game/${id}/AiDetections` : null,
    async (url: string) => {
      const response = await fetch(url, { credentials: 'same-origin' })
      if (!response.ok) throw new Error(`HTTP ${response.status}`)
      return response.json()
    },
    { refreshInterval: 15000 }
  )
  const { data: banned, mutate: refreshBans } = useSWR<number[]>(
    id ? `/api/game/${id}/AiBans` : null,
    async (url: string) => {
      const response = await fetch(url, { credentials: 'same-origin' })
      if (!response.ok) throw new Error(`HTTP ${response.status}`)
      return response.json()
    },
    { refreshInterval: 15000 }
  )
  const { data: ledger } = useSWR<AiMarkedFlagInfo[]>(
    id ? `/api/game/${id}/AiMarkedFlags` : null,
    async (url: string) => {
      const response = await fetch(url, { credentials: 'same-origin' })
      if (!response.ok) throw new Error(`HTTP ${response.status}`)
      return response.json()
    },
    { refreshInterval: 15000 }
  )
  const [busy, setBusy] = useState<number | null>(null)
  const [actionError, setActionError] = useState('')

  const updateBan = async (event: AiDetectionInfo, lift: boolean) => {
    const question = lift
      ? `解除 ${event.teamName} 的 Agent 解题封禁？`
      : `确认因使用 Agent 解题封禁 ${event.teamName}？这会暂停该队伍的参赛资格。`
    if (!window.confirm(question)) return
    setBusy(event.participationId)
    setActionError('')
    try {
      const response = await fetch(`/api/game/${id}/AiBan/${event.participationId}`, {
        method: lift ? 'DELETE' : 'POST',
        credentials: 'same-origin',
        headers: lift ? undefined : { 'Content-Type': 'application/json' },
        body: lift ? undefined : JSON.stringify({ detectionId: event.id }),
      })
      if (!response.ok) throw new Error(`HTTP ${response.status}`)
      if (!lift && !(await response.json()).instancesClosed)
        setActionError(isChinese ? '封禁已生效，但部分实例关闭失败，请手动检查' : 'Ban applied, but some instances could not be stopped')
      await refreshBans()
    } catch {
      setActionError('更新封禁状态失败')
    } finally {
      setBusy(null)
    }
  }

  return (
    <WithGameMonitor isLoading={!data && !error}>
      <Stack>
        <Group justify="space-between">
          <Title order={2}>{t('game.tab.monitor.ai_detection')}</Title>
          <Badge color="orange">{data?.length ?? 0}</Badge>
        </Group>
        <Alert color="orange">
          {isChinese
            ? '模型标识来自实例请求头，属于疑似 AI 使用线索，不能单独作为违规证据。共享实例只能定位到队伍。「已提交」表示队伍提交的值与下发的标记 flag 逐字符相等——下方台账并排列出了两个原值，可直接核对，但仍需人工复核。'
            : 'Model IDs come from instance request headers. They are review signals, not proof of a rule violation. Shared instances identify teams, not individual players. "Submitted" means the stored answer equals the issued decoy character for character — the ledger below shows both values side by side for verification, and still needs human review.'}
        </Alert>
        {error && <Alert color="red">{isChinese ? '加载检测记录失败' : 'Could not load detection events'}</Alert>}
        {actionError && <Alert color="red">{actionError}</Alert>}
        <Paper withBorder p="md">
          {!data?.length ? (
            <Text c="dimmed">{isChinese ? '暂无检测记录' : 'No detections yet'}</Text>
          ) : (
            <Table striped highlightOnHover>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>{isChinese ? '时间' : 'Time'}</Table.Th>
                  <Table.Th>{isChinese ? '队伍' : 'Team'}</Table.Th>
                  <Table.Th>{isChinese ? '题目' : 'Challenge'}</Table.Th>
                  <Table.Th>{isChinese ? '自报模型 / Agent' : 'Reported model / agent'}</Table.Th>
                  <Table.Th>{isChinese ? '标记 flag' : 'Marked flag'}</Table.Th>
                  <Table.Th>{isChinese ? '处置' : 'Action'}</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {data.map((event, index) => (
                  <Table.Tr key={`${event.id}-${index}`}>
                    <Table.Td>{dayjs(event.seenAt).format('YYYY-MM-DD HH:mm:ss')}</Table.Td>
                    <Table.Td>{event.teamName} (#{event.teamId})</Table.Td>
                    <Table.Td>{event.challengeTitle} (#{event.challengeId})</Table.Td>
                    <Table.Td><Text ff="monospace" size="sm">{event.modelId}</Text></Table.Td>
                    <Table.Td maw={280}>
                      <Group gap={4} wrap="nowrap">
                        {event.markedFlagSubmitted ? (
                          <Badge color="red" variant="filled">
                            {isChinese ? '已提交' : 'submitted'}
                          </Badge>
                        ) : event.markedFlagIssued ? (
                          <Badge color="orange" variant="light">
                            {isChinese ? '已下发' : 'issued'}
                          </Badge>
                        ) : (
                          <Text c="dimmed" size="sm">
                            —
                          </Text>
                        )}
                      </Group>
                      {event.markedFlagValue && (
                        <Text ff="monospace" size="xs" c="dimmed" truncate title={event.markedFlagValue}>
                          {event.markedFlagValue}
                        </Text>
                      )}
                    </Table.Td>
                    <Table.Td>
                      <Button size="xs" color={banned?.includes(event.participationId) ? 'gray' : 'red'}
                        loading={busy === event.participationId}
                        onClick={() => updateBan(event, banned?.includes(event.participationId) ?? false)}>
                        {banned?.includes(event.participationId) ? '解除封禁' : '使用 Agent 解题封禁'}
                      </Button>
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          )}
        </Paper>

        <Paper withBorder p="md">
          <Title order={5} mb={4}>
            {isChinese ? '标记 flag 台账（下发值 vs 提交值）' : 'Decoy ledger (issued vs submitted)'}
          </Title>
          <Text size="xs" c="dimmed" mb="sm">
            {isChinese
              ? '每一行是一次实际下发的标记 flag。若队伍提交过，右侧并排显示提交记录里的原始答案，可逐字符比对，无需信任徽标。'
              : 'Each row is one decoy actually issued. If it was submitted, the stored answer is shown next to it for a character-by-character check.'}
          </Text>
          {!ledger?.length ? (
            <Text c="dimmed">{isChinese ? '暂无标记 flag 下发记录' : 'No decoys issued yet'}</Text>
          ) : (
            <Table striped highlightOnHover>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>{isChinese ? '下发时间' : 'Issued at'}</Table.Th>
                  <Table.Th>{isChinese ? '队伍' : 'Team'}</Table.Th>
                  <Table.Th>{isChinese ? '题目' : 'Challenge'}</Table.Th>
                  <Table.Th>{isChinese ? '模型' : 'Model'}</Table.Th>
                  <Table.Th>{isChinese ? '下发的标记 flag' : 'Issued value'}</Table.Th>
                  <Table.Th>{isChinese ? '提交的 flag（原值）' : 'Submitted value'}</Table.Th>
                  <Table.Th>{isChinese ? '比对' : 'Match'}</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {ledger.map((row) => {
                  const matched = !!row.submittedAnswer && row.submittedAnswer === row.flag
                  return (
                    <Table.Tr key={row.id}>
                      <Table.Td>{dayjs(row.issuedAtUtc).format('MM-DD HH:mm:ss')}</Table.Td>
                      <Table.Td>{row.teamName}</Table.Td>
                      <Table.Td>{row.challengeTitle}</Table.Td>
                      <Table.Td>
                        <Text ff="monospace" size="sm">
                          {row.modelId}
                        </Text>
                      </Table.Td>
                      <Table.Td maw={280}>
                        <Text ff="monospace" size="xs" style={{ wordBreak: 'break-all' }}>
                          {row.flag}
                        </Text>
                      </Table.Td>
                      <Table.Td maw={280}>
                        {row.submittedAnswer ? (
                          <>
                            <Text ff="monospace" size="xs" style={{ wordBreak: 'break-all' }}>
                              {row.submittedAnswer}
                            </Text>
                            <Text size="xs" c="dimmed">
                              #{row.submissionId}
                              {row.submissionTime ? ` · ${dayjs(row.submissionTime).format('MM-DD HH:mm:ss')}` : ''}
                            </Text>
                          </>
                        ) : (
                          <Text c="dimmed" size="sm">
                            {isChinese ? '未提交' : 'not submitted'}
                          </Text>
                        )}
                      </Table.Td>
                      <Table.Td>
                        {row.submittedAnswer ? (
                          matched ? (
                            <Badge color="red" variant="filled">
                              {isChinese ? '逐字符一致' : 'exact match'}
                            </Badge>
                          ) : (
                            <Badge color="yellow" variant="filled">
                              {isChinese ? '不一致' : 'mismatch'}
                            </Badge>
                          )
                        ) : (
                          <Text c="dimmed" size="sm">
                            —
                          </Text>
                        )}
                      </Table.Td>
                    </Table.Tr>
                  )
                })}
              </Table.Tbody>
            </Table>
          )}
        </Paper>
      </Stack>
    </WithGameMonitor>
  )
}

export default AiDetection
