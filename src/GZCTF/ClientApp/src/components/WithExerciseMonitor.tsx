import { LoadingOverlay, Stack, Title } from '@mantine/core'
import { mdiExclamationThick, mdiFlag, mdiLightningBolt, mdiPackageVariant } from '@mdi/js'
import { Icon } from '@mdi/react'
import React, { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useLocation, useNavigate } from 'react-router'
import { IconTabs } from '@Components/IconTabs'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { DEFAULT_LOADING_OVERLAY } from '@Utils/Shared'
import { Role } from '@Api'

interface WithExerciseMonitorProps extends React.PropsWithChildren {
  isLoading?: boolean
}

export const WithExerciseMonitor: FC<WithExerciseMonitorProps> = ({ children, isLoading }) => {
  const navigate = useNavigate()
  const location = useLocation()
  const { t } = useTranslation()

  const tabs = [
    {
      tabKey: 'events',
      label: t('exercise.tab.monitor.events'),
      icon: <Icon path={mdiLightningBolt} size={1} />,
    },
    {
      tabKey: 'submissions',
      label: t('exercise.tab.monitor.submissions'),
      icon: <Icon path={mdiFlag} size={1} />,
    },
    {
      tabKey: 'cheatinfo',
      label: t('exercise.tab.monitor.cheatinfo'),
      icon: <Icon path={mdiExclamationThick} size={1} />,
    },
    {
      tabKey: 'instances',
      label: t('exercise.tab.monitor.instances'),
      icon: <Icon path={mdiPackageVariant} size={1} />,
    },
  ]

  const getTab = (path: string) => tabs.find((tab) => path.endsWith(tab.tabKey))

  const [activeTab, setActiveTab] = useState(getTab(location.pathname)?.tabKey ?? tabs[0].tabKey)

  useEffect(() => {
    const tab = getTab(location.pathname)
    if (tab) {
      setActiveTab(tab.tabKey)
    } else {
      navigate('/exercise/monitor/events')
    }
  }, [location])

  return (
    <WithNavBar width="90%">
      <WithRole requiredRole={Role.Monitor}>
        <Stack pos="relative" mt="md">
          <IconTabs
            active={tabs.findIndex((tab) => tab.tabKey === activeTab)}
            onTabChange={(_, tabKey) => navigate(`/exercise/monitor/${tabKey}`)}
            tabs={tabs}
            aside={<Title>{t('exercise.tab.monitor.index')}</Title>}
          />
          <LoadingOverlay visible={isLoading ?? false} overlayProps={DEFAULT_LOADING_OVERLAY} />
          {children}
        </Stack>
      </WithRole>
    </WithNavBar>
  )
}
