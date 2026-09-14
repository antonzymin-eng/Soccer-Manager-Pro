from pathlib import Path

p = Path('.github/w6-controlled-ball-patch.py')
s = p.read_text()
start_marker = 'replace_once(\n    path,\n    """                case TackleOutcome.BallWon:'
end_marker = 'replace_once(\n    path,\n    """            _possessingAgentId = MatchEngineConstants.NO_POSSESSION;'
start = s.index(start_marker)
end = s.index(end_marker, start)
replacement = '''replace_once(
    path,
    """                    _possessingAgentId = tackler;\n                    ClearPassInFlight();\n                    _tackleFlag[carrier] = true;\n                    _tackleWonCount++;\n""",
    """                    TakeControlledPossession(tackler);\n                    ClearPassInFlight();\n                    _tackleFlag[carrier] = true;\n                    _tackleWonCount++;\n""",
)
replace_once(
    path,
    """                    _possessingAgentId = MatchEngineConstants.NO_POSSESSION;\n                    ClearPassInFlight();\n                    _tackleFlag[carrier] = true;\n                    _tackleLooseCount++;\n""",
    """                    ReleaseControlledPossession(placeAtGround: false);\n                    ClearPassInFlight();\n                    _tackleFlag[carrier] = true;\n                    _tackleLooseCount++;\n""",
)
'''
p.write_text(s[:start] + replacement + s[end:])
