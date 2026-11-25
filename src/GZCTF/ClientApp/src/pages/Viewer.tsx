import { Button, Center, Group, Loader, Stack, Text, Title } from '@mantine/core'
import { mdiAccountGroupOutline, mdiOpenInNew, mdiRefresh } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useCallback, useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { WithNavBar } from '@Components/WithNavbar'
import { usePageTitle } from '@Hooks/usePageTitle'

type FailureKind = 'team-required' | 'unavailable' | 'unknown'

const Viewer: FC = () => {
  const { t } = useTranslation()
  const navigate = useNavigate()

  const [failure, setFailure] = useState<FailureKind | null>(null)
  const [detail, setDetail] = useState<string | null>(null)

  usePageTitle(t('common.tab.viewer'))

  const requestHandoff = useCallback(async () => {
    setFailure(null)
    setDetail(null)

    try {
      const response = await fetch('/api/viewer/sso', {
        method: 'GET',
        credentials: 'same-origin',
        cache: 'no-store',
        headers: { Accept: 'application/json' },
      })

      if (response.status === 401) {
        navigate('/account/login?from=/viewer', { replace: true })
        return
      }

      const body = (await response.json().catch(() => null)) as { url?: string; message?: string } | null

      if (response.ok && body?.url) {
        window.location.replace(body.url)
        return
      }

      if (response.status === 400) {
        setFailure('team-required')
        return
      }

      setFailure(response.status >= 500 ? 'unavailable' : 'unknown')
      setDetail(body?.message ?? null)
    } catch {
      setFailure('unavailable')
    }
  }, [navigate])

  useEffect(() => {
    void requestHandoff()
  }, [requestHandoff])

  const message = useMemo(() => {
    if (failure === 'team-required') return t('common.content.viewer.team_required')
    if (failure === 'unavailable') return t('common.content.viewer.unavailable')
    if (failure === 'unknown') return t('common.content.viewer.failed')
    return null
  }, [failure, t])

  return (
    <WithNavBar minWidth={0} isLoading={!failure}>
      <Center h="60vh">
        <Stack align="center" gap="md" maw={560}>
          {failure ? (
            <>
              <Icon path={failure === 'team-required' ? mdiAccountGroupOutline : mdiOpenInNew} size={3} />
              <Title order={2}>{t('common.tab.viewer')}</Title>
              <Text ta="center" c="dimmed">
                {message}
              </Text>
              {detail && (
                <Text ta="center" size="xs" c="dimmed">
                  {detail}
                </Text>
              )}
              <Group>
                {failure === 'team-required' ? (
                  <Button component="a" href="/teams" leftSection={<Icon path={mdiAccountGroupOutline} size={0.8} />}>
                    {t('common.content.viewer.go_team')}
                  </Button>
                ) : (
                  <Button onClick={() => void requestHandoff()} leftSection={<Icon path={mdiRefresh} size={0.8} />}>
                    {t('common.button.retry')}
                  </Button>
                )}
              </Group>
            </>
          ) : (
            <>
              <Loader />
              <Text c="dimmed">{t('common.content.viewer.redirecting')}</Text>
            </>
          )}
        </Stack>
      </Center>
    </WithNavBar>
  )
}

export default Viewer
