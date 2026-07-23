import { useCallback, useState } from 'react'
import { ContractsPage } from './ContractsPage'
import { ProfilesPage } from './ProfilesPage'

const ACTIVE_CONTRACT_KEY = 'atb.activeContractId'

type HomePageProps = {
  onError: (message: string | null) => void
}

export function HomePage({ onError }: HomePageProps) {
  const [contractId, setContractId] = useState<string | null>(() => {
    // Always start on contract selection for a clean demo entry.
    localStorage.removeItem(ACTIVE_CONTRACT_KEY)
    return null
  })

  const handleSelectContract = useCallback(
    (id: string) => {
      localStorage.setItem(ACTIVE_CONTRACT_KEY, id)
      setContractId(id)
      onError(null)
    },
    [onError],
  )

  const handleChangeContract = useCallback(() => {
    localStorage.removeItem(ACTIVE_CONTRACT_KEY)
    setContractId(null)
    onError(null)
  }, [onError])

  if (!contractId) {
    return <ContractsPage onSelect={handleSelectContract} onError={onError} />
  }

  return (
    <ProfilesPage
      contractId={contractId}
      onChangeContract={handleChangeContract}
      onError={onError}
    />
  )
}
