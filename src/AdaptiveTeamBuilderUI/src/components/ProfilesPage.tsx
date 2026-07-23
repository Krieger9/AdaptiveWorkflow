import { useEffect, useState, type FormEvent } from 'react'
import {
  getEmployeeProfile,
  searchEmployeeProfiles,
  type EmployeeProfile,
  type EmployeeProfileListItem,
  type PositionType,
} from '../api/client'
import './ProfilesPage.css'

const POSITION_FILTERS: { value: PositionType; label: string }[] = [
  { value: 'Developer', label: 'Developers' },
  { value: 'UxDesigner', label: 'UX Designers' },
  { value: 'Product', label: 'Product Teams' },
  { value: 'QualityAssurance', label: 'Quality Assurance' },
]

function formatPositionType(type: string): string {
  switch (type) {
    case 'Developer':
      return 'Developer'
    case 'UxDesigner':
      return 'UX Designer'
    case 'Product':
      return 'Product'
    case 'QualityAssurance':
      return 'Quality Assurance'
    default:
      return type
  }
}

function formatSpecialty(value: string | null): string | null {
  if (!value) {
    return null
  }
  switch (value) {
    case 'ScrumMaster':
      return 'Scrum Master'
    case 'BusinessAnalyst':
      return 'Business Analyst'
    case 'ProductOwner':
      return 'Product Owner'
    default:
      return value
  }
}

type ProfilesPageProps = {
  onError: (message: string | null) => void
}

export function ProfilesPage({ onError }: ProfilesPageProps) {
  const [selectedTypes, setSelectedTypes] = useState<PositionType[]>([])
  const [searchText, setSearchText] = useState('')
  const [hasSearched, setHasSearched] = useState(false)
  const [busy, setBusy] = useState(false)
  const [results, setResults] = useState<EmployeeProfileListItem[]>([])
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [selectedProfile, setSelectedProfile] = useState<EmployeeProfile | null>(null)
  const [loadingDetail, setLoadingDetail] = useState(false)

  useEffect(() => {
    if (!selectedId) {
      setSelectedProfile(null)
      return
    }

    let cancelled = false
    setLoadingDetail(true)
    getEmployeeProfile(selectedId)
      .then((profile) => {
        if (!cancelled) {
          setSelectedProfile(profile)
        }
      })
      .catch((err) => {
        if (!cancelled) {
          onError(err instanceof Error ? err.message : 'Failed to load profile')
          setSelectedProfile(null)
        }
      })
      .finally(() => {
        if (!cancelled) {
          setLoadingDetail(false)
        }
      })

    return () => {
      cancelled = true
    }
  }, [onError, selectedId])

  function toggleType(type: PositionType) {
    setSelectedTypes((current) =>
      current.includes(type)
        ? current.filter((item) => item !== type)
        : [...current, type],
    )
  }

  async function handleSearch(event: FormEvent) {
    event.preventDefault()
    onError(null)

    if (!searchText.trim() && selectedTypes.length === 0) {
      onError('Enter a search term or select at least one position type.')
      return
    }

    setBusy(true)
    setHasSearched(true)
    setSelectedId(null)
    try {
      const items = await searchEmployeeProfiles({
        q: searchText,
        positionTypes: selectedTypes,
      })
      setResults(items)
    } catch (err) {
      setResults([])
      onError(err instanceof Error ? err.message : 'Search failed')
    } finally {
      setBusy(false)
    }
  }

  function handleClear() {
    setSearchText('')
    setSelectedTypes([])
    setResults([])
    setSelectedId(null)
    setSelectedProfile(null)
    setHasSearched(false)
    onError(null)
  }

  return (
    <section className="profiles">
      <header className="profiles-header">
        <h1>Contractor profiles</h1>
        <p className="muted">
          Search internal resumes by skills and position type. Results stay empty
          until you run a search.
        </p>
      </header>

      <form className="profiles-filters" onSubmit={handleSearch}>
        <label className="profiles-search">
          Search
          <input
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
            placeholder="Name, title, skills…"
          />
        </label>

        <fieldset className="profiles-types">
          <legend>Position types</legend>
          <div className="profiles-type-row">
            {POSITION_FILTERS.map((filter) => (
              <label key={filter.value} className="profiles-type-chip">
                <input
                  type="checkbox"
                  checked={selectedTypes.includes(filter.value)}
                  onChange={() => toggleType(filter.value)}
                />
                {filter.label}
              </label>
            ))}
          </div>
        </fieldset>

        <div className="profiles-actions">
          <button type="button" className="secondary" onClick={handleClear}>
            Clear
          </button>
          <button type="submit" disabled={busy}>
            {busy ? 'Searching…' : 'Search'}
          </button>
        </div>
      </form>

      <div className="profiles-layout">
        <div className="profiles-list-panel">
          <h2>Results</h2>
          {!hasSearched && (
            <div className="profiles-empty-box">
              Run a search to load matching contractor profiles.
            </div>
          )}
          {hasSearched && results.length === 0 && !busy && (
            <div className="profiles-empty-box">No profiles matched your filters.</div>
          )}
          {hasSearched && results.length > 0 && (
            <ul className="profiles-list">
              {results.map((item) => (
                <li key={item.id}>
                  <button
                    type="button"
                    className={
                      item.id === selectedId
                        ? 'profiles-list-item active'
                        : 'profiles-list-item'
                    }
                    onClick={() => setSelectedId(item.id)}
                  >
                    <span className="profiles-list-name">{item.displayName}</span>
                    <span className="profiles-list-meta">
                      {formatPositionType(item.positionType)}
                      {item.level ? ` · ${item.level}` : ''}
                      {item.roleSpecialty
                        ? ` · ${formatSpecialty(item.roleSpecialty)}`
                        : ''}
                    </span>
                    <span className="profiles-list-title">{item.title}</span>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="profiles-detail-panel">
          <h2>Profile</h2>
          {!selectedId && (
            <div className="profiles-empty-box">
              Select a profile from the results to view details.
            </div>
          )}
          {selectedId && loadingDetail && (
            <div className="profiles-empty-box">Loading profile…</div>
          )}
          {selectedId && !loadingDetail && selectedProfile && (
            <EmployeeProfileCard profile={selectedProfile} />
          )}
        </div>
      </div>
    </section>
  )
}

function EmployeeProfileCard({ profile }: { profile: EmployeeProfile }) {
  return (
    <article className="profile-card">
      <header>
        <h3>{profile.displayName}</h3>
        <p className="profile-card-title">{profile.title}</p>
      </header>

      <dl className="profile-card-meta">
        <div>
          <dt>Position type</dt>
          <dd>{formatPositionType(profile.positionType)}</dd>
        </div>
        {profile.roleSpecialty && (
          <div>
            <dt>Specialty</dt>
            <dd>{formatSpecialty(profile.roleSpecialty)}</dd>
          </div>
        )}
        {profile.level && (
          <div>
            <dt>Level</dt>
            <dd>{profile.level}</dd>
          </div>
        )}
        {profile.yearsExperience != null && (
          <div>
            <dt>Experience</dt>
            <dd>{profile.yearsExperience} years</dd>
          </div>
        )}
        {profile.location && (
          <div>
            <dt>Location</dt>
            <dd>{profile.location}</dd>
          </div>
        )}
        {profile.availability && (
          <div>
            <dt>Availability</dt>
            <dd>{profile.availability}</dd>
          </div>
        )}
      </dl>

      {profile.summary && (
        <section>
          <h4>Summary</h4>
          <p>{profile.summary}</p>
        </section>
      )}

      {profile.skills.length > 0 && (
        <section>
          <h4>Skills</h4>
          <ul className="profile-skills">
            {profile.skills.map((skill) => (
              <li key={skill}>{skill}</li>
            ))}
          </ul>
        </section>
      )}
    </article>
  )
}
