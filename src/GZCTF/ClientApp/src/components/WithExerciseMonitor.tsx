import { Group, LoadingOverlay, Stack, Tabs } from '@mantine/core'
import { mdiExclamationThick, mdiFlag, mdiLightningBolt, mdiPackageVariant } from '@mdi/js'
import { Icon } from '@mdi/react'
import React, { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useLocation, useNavigate } from 'react-router'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { DEFAULT_LOADING_OVERLAY } from '@Utils/Shared'
import { Role } from '@Api'
import misc from '@Styles/Misc.module.css'

interface WithExerciseMonitorProps extends React.PropsWithChildren {
  isLoading?: boolean
}

export const WithExerciseMonitor: FC<WithExerciseMonitorProps> = ({ children, isLoading }) => {
  const navigate = useNavigate()
  const location = useLocation()
  const { t } = useTranslation()

  const pages = [
    { icon: mdiLightningBolt, title: t('exercise.tab.monitor.events'), path: 'events' },
    { icon: mdiFlag, title: t('exercise.tab.monitor.submissions'), path: 'submissions' },
    { icon: mdiExclamationThick, title: t('exercise.tab.monitor.cheatinfo'), path: 'cheatinfo' },
    { icon: mdiPackageVariant, title: t('exercise.tab.monitor.instances'), path: 'instances' },
  ]

  const getTab = (path: string) => pages.find((page) => path.endsWith(page.path))

  const [activeTab, setActiveTab] = useState(getTab(location.pathname)?.path ?? pages[0].path)

  useEffect(() => {
    const tab = getTab(location.pathname)
    if (tab) {
      setActiveTab(tab.path ?? '')
    } else {
      navigate('/exercise/monitor/events')
    }
  }, [location])

  return (
    <WithNavBar width="90%">
      <WithRole requiredRole={Role.Monitor}>
        <Group justify="space-between" align="flex-start">
          <Stack>
            <Tabs
              orientation="vertical"
              value={activeTab}
              onChange={(value) => value && navigate(`/exercise/monitor/${value}`)}
              classNames={{
                root: misc.w10rem,
                list: misc.w10rem,
              }}
            >
              <Tabs.List>
                {pages.map((page) => (
                  <Tabs.Tab key={page.path} leftSection={<Icon path={page.icon} size={1} />} value={page.path}>
                    {page.title}
                  </Tabs.Tab>
                ))}
              </Tabs.List>
            </Tabs>
          </Stack>
          <Stack w="calc(100% - 11rem)" pos="relative">
            <LoadingOverlay visible={isLoading ?? false} overlayProps={DEFAULT_LOADING_OVERLAY} />
            {children}
          </Stack>
        </Group>
      </WithRole>
    </WithNavBar>
  )
}
