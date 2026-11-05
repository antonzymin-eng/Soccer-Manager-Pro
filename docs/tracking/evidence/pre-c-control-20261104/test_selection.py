"""Created 2026-11-04: adversarial selection-rule checks independent of corpus."""
import unittest
from analyze import windows

def row(kind,frame,order,agent=0,team=0,**extra):
    return dict(kind=kind,frame=frame,order=order,agent=agent,team=team,**extra)

class SelectionTests(unittest.TestCase):
    def test_loose_tackle_outcome_does_not_select_an_opponent_window(self):
        result=windows([row('release',1,1),row('touch:tackle-loose',2,1,11,1),
                        row('touch:first-touch',3,1,3,0)],1000)[0]
        self.assertEqual(result['reason'],'teammate-first')
        self.assertEqual(result['firstOtherTouch']['agent'],3)
        self.assertFalse(result['selected'])
    def test_won_tackle_is_a_recorded_physical_acquisition(self):
        result=windows([row('release',1,1),row('touch:tackle-won',2,1,11,1)],1000)[0]
        self.assertTrue(result['selected'])
    def test_unknown_touch_kind_cannot_silently_change_selection(self):
        with self.assertRaisesRegex(ValueError,'Unregistered'):
            windows([row('release',1,1),row('touch:future-producer',2,1,11,1)],1000)
    def test_full_time_truncates_followup_and_censors_pending_release(self):
        import base64
        end=row('event',500,1,ordinal=26,raw=base64.b64encode(bytes(12)+b'\x01').decode())
        result=windows([row('release',1,1),row('touch:first-touch',2,1,11,1),end],1000)[0]
        self.assertFalse(result['complete']);self.assertEqual(result['observedFollowingFrames'],497)
        self.assertEqual(windows([row('release',1,1),end],1000)[0]['reason'],'full-time')
    def test_same_frame_order_and_goal_credit(self):
        rows=[row('release',100,1),row('shot',100,2,11,1),row('touch:body-applied',100,3,11,1),
              row('shot',100,4,11,1),row('goal',100,5,0,0,scoringTeam=1)]
        result=windows(rows,1000)[0]
        self.assertEqual([r['order'] for r in result['events']],[4,5])
        self.assertEqual([r['side'] for r in result['events']],['opponent','opponent'])
        self.assertTrue(result['complete'])
    def test_first_teammate_excludes_later_opponent(self):
        result=windows([row('release',1,1),row('touch:first-touch',2,1,3,0),row('touch:body-applied',3,1,11,1)],1000)[0]
        self.assertFalse(result['selected']);self.assertEqual(result['reason'],'teammate-first')
    def test_own_touches_preserved_and_507_is_inclusive(self):
        result=windows([row('release',1,1),row('touch:pass-contact',2,1),row('touch:body-applied',508,1,11,1)],2000)[0]
        self.assertTrue(result['selected']);self.assertEqual(len(result['ownTouches']),1)
        self.assertFalse(windows([row('release',1,1),row('touch:body-applied',509,1,11,1)],2000)[0]['selected'])
    def test_restart_censors_before_later_touch(self):
        result=windows([row('release',1,1),row('restriction-end:restart',2,1),row('touch:body-applied',3,1,11,1)],1000)[0]
        self.assertEqual(result['reason'],'restart');self.assertFalse(result['selected'])
    def test_end_inclusive_truncation_and_no_later_event(self):
        rows=[row('release',1,1),row('touch:body-applied',2,1,11,1),row('shot',602,1,11,1),row('shot',603,1,11,1)]
        result=windows(rows,602)[0];self.assertTrue(result['complete']);self.assertEqual(len(result['events']),1)
        result=windows(rows[:2],601)[0];self.assertFalse(result['complete']);self.assertEqual(result['observedFollowingFrames'],599)
    def test_overlap_preserves_membership(self):
        rows=[row('release',1,1),row('touch:body-applied',2,1,11,1),row('release',3,1),
              row('touch:body-applied',4,1,11,1),row('shot',5,1,11,1)]
        result=windows(rows,1000);self.assertEqual([len(x['events']) for x in result],[1,1])
    def test_rejected_attempt_is_not_a_touch(self):
        result=windows([row('release',1,1),row('hand-attempt',2,1,11,1)],1000)[0]
        self.assertEqual(result['reason'],'run-end');self.assertIsNone(result['firstOtherTouch'])

if __name__=='__main__': unittest.main()
