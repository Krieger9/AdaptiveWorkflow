import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import {
  addTeamMember,
  createTeam,
  getEmployeeProfile,
  getTeam,
  hideTeamProfile,
  listTeams,
  removeTeamMember,
  renameTeam,
  searchEmployeeProfiles,
  upsertTeamRequirements,
  type EmployeeProfile,
  type EmployeeProfileListItem,
  type PositionType,
  type TeamDetail,
  type TeamListItem,
} from '../api/client'
import './ProfilesPage.css'

const POSITION_FILTERS: { value: PositionType; label: string }[] = [
  { value: 'Developer', label: 'Developers' },
  { value: 'UxDesigner', label: 'UX Designers' },
  { value: 'Product', label: 'Product Teams' },
  { value: 'QualityAssurance', label: 'Quality Assurance' },
]

const ACTIVE_TEAM_KEY = 'atb.activeTeamId'

type TeamPanelMode = 'idle' | 'openExisting'

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
  const [teams, setTeams] = useState<TeamListItem[]>([])
  const [activeTeamId, setActiveTeamId] = useState<string | null>(null)
  const [team, setTeam] = useState<TeamDetail | null>(null)
  const [teamPanelMode, setTeamPanelMode] = useState<TeamPanelMode>('idle')
  const [newTeamName, setNewTeamName] = useState('')
  const [renameValue, setRenameValue] = useState('')
  const [requirementDrafts, setRequirementDrafts] = useState<Record<string, number>>({})
  const [showRename, setShowRename] = useState(false)

  const [selectedTypes, setSelectedTypes] = useState<PositionType[]>([])
  const [searchText, setSearchText] = useState('')
  const [hasSearched, setHasSearched] = useState(false)
  const [busy, setBusy] = useState(false)
  const [teamBusy, setTeamBusy] = useState(false)
  const [results, setResults] = useState<EmployeeProfileListItem[]>([])
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [selectedProfile, setSelectedProfile] = useState<EmployeeProfile | null>(null)
  const [loadingDetail, setLoadingDetail] = useState(false)

  const memberIds = useMemo(
    () => new Set(team?.members.map((m) => m.employeeProfileId) ?? []),
    [team],
  )

  const refreshTeams = useCallback(async () => {
    const items = await listTeams()
    setTeams(items)
    return items
  }, [])

  const loadTeam = useCallback(async (teamId: string) => {
    const detail = await getTeam(teamId)
    setTeam(detail)
    setRenameValue(detail.name)
    setShowRename(false)
    setTeamPanelMode('idle')
    const drafts: Record<string, number> = {}
    for (const req of detail.requirements) {
      drafts[req.positionType] = req.requiredCount
    }
    setRequirementDrafts(drafts)
    localStorage.setItem(ACTIVE_TEAM_KEY, teamId)
    setActiveTeamId(teamId)
    return detail
  }, [])

  function closeActiveTeam() {
    setTeam(null)
    setActiveTeamId(null)
    setRenameValue('')
    setRequirementDrafts({})
    setShowRename(false)
    setTeamPanelMode('idle')
    setNewTeamName('')
    localStorage.removeItem(ACTIVE_TEAM_KEY)
    setSearchText('')
    setSelectedTypes([])
    setResults([])
    setSelectedId(null)
    setSelectedProfile(null)
    setHasSearched(false)
    onError(null)
  }

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        await refreshTeams()
        if (cancelled) {
          return
        }
        // Start with no active team selected.
        localStorage.removeItem(ACTIVE_TEAM_KEY)
        setActiveTeamId(null)
        setTeam(null)
      } catch (err) {
        if (!cancelled) {
          onError(err instanceof Error ? err.message : 'Failed to load teams')
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [onError, refreshTeams])

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
        teamId: activeTeamId,
      })
      setResults(items)
    } catch (err) {
      setResults([])
      onError(err instanceof Error ? err.message : 'Search failed')
    } finally {
      setBusy(false)
    }
  }

  function handleClearSearch() {
    setSearchText('')
    setSelectedTypes([])
    setResults([])
    setSelectedId(null)
    setSelectedProfile(null)
    setHasSearched(false)
    onError(null)
  }

  async function handleCreateTeam(event: FormEvent) {
    event.preventDefault()
    onError(null)
    if (!newTeamName.trim()) {
      onError('Enter a unique team name.')
      return
    }
    setTeamBusy(true)
    try {
      const created = await createTeam(newTeamName.trim())
      setNewTeamName('')
      await refreshTeams()
      await loadTeam(created.id)
      handleClearSearch()
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Failed to create team')
    } finally {
      setTeamBusy(false)
    }
  }

  async function handleSwitchTeam(teamId: string) {
    onError(null)
    setTeamBusy(true)
    try {
      await loadTeam(teamId)
      handleClearSearch()
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Failed to load team')
    } finally {
      setTeamBusy(false)
    }
  }

  function startCreateNewTeam() {
    closeActiveTeam()
    setTeamPanelMode('idle')
  }

  function startOpenExisting() {
    setTeamPanelMode('openExisting')
  }

  async function handleRenameTeam(event: FormEvent) {
    event.preventDefault()
    if (!team) {
      return
    }
    onError(null)
    setTeamBusy(true)
    try {
      const updated = await renameTeam(team.id, renameValue.trim())
      setTeam(updated)
      await refreshTeams()
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Failed to rename team')
    } finally {
      setTeamBusy(false)
    }
  }

  async function handleSaveRequirements(event: FormEvent) {
    event.preventDefault()
    if (!team) {
      return
    }
    onError(null)
    setTeamBusy(true)
    try {
      const requirements = team.requirements.map((req) => ({
        positionType: req.positionType,
        requiredCount: requirementDrafts[req.positionType] ?? req.requiredCount,
      }))
      const updated = await upsertTeamRequirements(team.id, requirements)
      setTeam(updated)
      const drafts: Record<string, number> = {}
      for (const req of updated.requirements) {
        drafts[req.positionType] = req.requiredCount
      }
      setRequirementDrafts(drafts)
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Failed to save requirements')
    } finally {
      setTeamBusy(false)
    }
  }

  async function handleSelectPerson(profileId: string) {
    if (!team) {
      onError('Create or select a team first.')
      return
    }
    onError(null)
    setTeamBusy(true)
    try {
      const updated = await addTeamMember(team.id, profileId)
      setTeam(updated)
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Failed to add team member')
    } finally {
      setTeamBusy(false)
    }
  }

  async function handleHidePerson(profileId: string) {
    if (!team) {
      onError('Create or select a team first.')
      return
    }
    onError(null)
    setTeamBusy(true)
    try {
      const updated = await hideTeamProfile(team.id, profileId)
      setTeam(updated)
      setResults((current) => current.filter((item) => item.id !== profileId))
      if (selectedId === profileId) {
        setSelectedId(null)
      }
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Failed to hide profile')
    } finally {
      setTeamBusy(false)
    }
  }

  async function handleRemoveMember(profileId: string) {
    if (!team) {
      return
    }
    onError(null)
    setTeamBusy(true)
    try {
      const updated = await removeTeamMember(team.id, profileId)
      setTeam(updated)
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Failed to remove team member')
    } finally {
      setTeamBusy(false)
    }
  }

  return (
    <section className="team-builder">
      <div className="team-builder-left">
        <header className="profiles-header">
          <h1>Find contractors</h1>
          <p className="muted">
            Search resumes, then hide or add people to the team on the right.
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
            <button type="button" className="secondary" onClick={handleClearSearch}>
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
                {results.map((item) => {
                  const onTeam = memberIds.has(item.id)
                  return (
                    <li key={item.id}>
                      <div
                        className={
                          item.id === selectedId
                            ? 'profiles-list-item active'
                            : 'profiles-list-item'
                        }
                      >
                        <button
                          type="button"
                          className="profiles-list-main"
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
                        <div className="profiles-list-actions">
                          <button
                            type="button"
                            className="secondary"
                            disabled={!team || teamBusy}
                            onClick={() => handleHidePerson(item.id)}
                          >
                            Hide
                          </button>
                          <button
                            type="button"
                            disabled={!team || teamBusy || onTeam}
                            onClick={() => handleSelectPerson(item.id)}
                          >
                            {onTeam ? 'On team' : 'Select'}
                          </button>
                        </div>
                      </div>
                    </li>
                  )
                })}
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
      </div>

      <aside className="team-builder-right">
        <header className="team-panel-header">
          <div>
            <h1>{team ? team.name : 'Team'}</h1>
            <p className="muted">
              {team
                ? 'Headcount targets and selected people for this team.'
                : 'Create a team or open an existing one to begin.'}
            </p>
          </div>
          {team && (
            <nav className="team-panel-nav" aria-label="Team actions">
              <button
                type="button"
                className="linkish"
                disabled={teamBusy}
                onClick={() => setShowRename((value) => !value)}
              >
                {showRename ? 'Cancel rename' : 'Rename'}
              </button>
              <button
                type="button"
                className="linkish"
                disabled={teamBusy}
                onClick={startOpenExisting}
              >
                Switch
              </button>
              <button
                type="button"
                className="linkish"
                disabled={teamBusy}
                onClick={startCreateNewTeam}
              >
                New
              </button>
              <button
                type="button"
                className="linkish"
                disabled={teamBusy}
                onClick={closeActiveTeam}
              >
                Close
              </button>
            </nav>
          )}
        </header>

        {!team && teamPanelMode === 'idle' && (
          <div className="team-start">
            <form className="team-create" onSubmit={handleCreateTeam}>
              <label>
                New team name
                <input
                  value={newTeamName}
                  onChange={(e) => setNewTeamName(e.target.value)}
                  placeholder="Unique team name"
                  maxLength={200}
                  required
                />
              </label>
              <button type="submit" disabled={teamBusy || !newTeamName.trim()}>
                Create team
              </button>
            </form>
            <button
              type="button"
              className="secondary team-open-existing"
              disabled={teamBusy || teams.length === 0}
              onClick={startOpenExisting}
            >
              {teams.length === 0 ? 'No existing teams yet' : 'Open existing team'}
            </button>
          </div>
        )}

        {!team && teamPanelMode === 'openExisting' && (
          <div className="team-start">
            <label className="team-switch">
              Choose a team
              <select
                value=""
                disabled={teams.length === 0 || teamBusy}
                onChange={(e) => {
                  if (e.target.value) {
                    void handleSwitchTeam(e.target.value)
                  }
                }}
              >
                <option value="">Select…</option>
                {teams.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name}
                  </option>
                ))}
              </select>
            </label>
            <button
              type="button"
              className="secondary"
              disabled={teamBusy}
              onClick={() => setTeamPanelMode('idle')}
            >
              Back to create
            </button>
          </div>
        )}

        {team && teamPanelMode === 'openExisting' && (
          <div className="team-start compact">
            <label className="team-switch">
              Switch to
              <select
                value={activeTeamId ?? ''}
                disabled={teamBusy}
                onChange={(e) => {
                  if (e.target.value) {
                    void handleSwitchTeam(e.target.value)
                  }
                }}
              >
                {teams.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name}
                  </option>
                ))}
              </select>
            </label>
            <button
              type="button"
              className="secondary"
              disabled={teamBusy}
              onClick={() => setTeamPanelMode('idle')}
            >
              Cancel
            </button>
          </div>
        )}

        {team && teamPanelMode === 'idle' && (
          <>
            {showRename && (
              <form className="team-rename" onSubmit={handleRenameTeam}>
                <label>
                  Team name
                  <input
                    value={renameValue}
                    onChange={(e) => setRenameValue(e.target.value)}
                    maxLength={200}
                    required
                  />
                </label>
                <button type="submit" disabled={teamBusy || !renameValue.trim()}>
                  Save name
                </button>
              </form>
            )}

            <form className="team-requirements" onSubmit={handleSaveRequirements}>
              <h2>Headcount needed</h2>
              <ul>
                {team.requirements.map((req) => {
                  const draft = requirementDrafts[req.positionType] ?? req.requiredCount
                  const met = req.selectedCount >= draft && draft > 0
                  const under = draft > 0 && req.selectedCount < draft
                  const statusClass = draft === 0 ? '' : met ? 'met' : under ? 'under' : ''
                  return (
                    <li key={req.positionType} className={statusClass}>
                      <div className="req-label">
                        <span className="req-marker" aria-hidden="true" />
                        <span>{req.positionTypeName}</span>
                      </div>
                      <input
                        type="number"
                        min={0}
                        value={draft}
                        onChange={(e) =>
                          setRequirementDrafts((current) => ({
                            ...current,
                            [req.positionType]: Math.max(0, Number(e.target.value) || 0),
                          }))
                        }
                      />
                      <span className="req-count">
                        {req.selectedCount}/{draft}
                      </span>
                    </li>
                  )
                })}
              </ul>
              <button type="submit" disabled={teamBusy}>
                Save headcount
              </button>
            </form>

            <div className="team-members">
              <h2>Selected people</h2>
              {team.members.length === 0 ? (
                <div className="profiles-empty-box">No one selected yet.</div>
              ) : (
                <ul className="team-member-list">
                  {team.members.map((member) => (
                    <li key={member.employeeProfileId}>
                      <div>
                        <strong>{member.displayName}</strong>
                        <div className="profiles-list-meta">
                          {formatPositionType(member.positionType)}
                          {member.level ? ` · ${member.level}` : ''}
                          {member.roleSpecialty
                            ? ` · ${formatSpecialty(member.roleSpecialty)}`
                            : ''}
                        </div>
                        <div className="profiles-list-title">{member.title}</div>
                      </div>
                      <button
                        type="button"
                        className="secondary"
                        disabled={teamBusy}
                        onClick={() => handleRemoveMember(member.employeeProfileId)}
                      >
                        Remove
                      </button>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </>
        )}
      </aside>
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
