Feature: State Monitoring metrics publish values with restart

	Scenario: Restart with partial single known occurence and duration
		Given a bar property of StandBy/Running enum type
		And a foo metric defined as
		  | metric                                                                           |
		  | Metric(foo).Is.Every(30).Seconds.OnAWindowOf(120).Seconds.StateMonitoringOf(bar) |
		When the application starts at 03:00:00
		Then the following publications occur
		  | time     | bar     | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
		  | 03:00:00 | StandBy |                        |                      |                        |                      |
		  | 03:00:30 |         | 1                      | 30                   | 0                      | 0                    |
		  | 03:00:42 | Running |                        |                      |                        |                      |
		When the application restarts at 03:00:55
		Then the following publications occur
		  | time     | bar | start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
		  | 03:01:00 |     | 02:59:00 | 03:01:00 | 1                      | 42                   | 0                      | 0                    |
		  | 03:01:30 |     | 02:59:30 | 03:01:30 | 1                      | 42                   | 0                      | 0                    |
		  | 03:02:00 |     | 03:00:00 | 03:02:00 | 1                      | 42                   | 0                      | 0                    |
		  | 03:02:30 |     | 03:00:30 | 03:02:30 | 1                      | 42-30                | 0                      | 0                    |
		  | 03:03:00 |     | 03:01:00 | 03:03:00 | 0                      | 0                    | 0                      | 0                    |

	Scenario: Restart with full known occurence and duration
		Given a bar property of StandBy/Running enum type
		And a foo metric defined as
		  | metric                                                                           |
		  | Metric(foo).Is.Every(30).Seconds.OnAWindowOf(120).Seconds.StateMonitoringOf(bar) |
		When the application starts at 03:00:00
		Then the following publications occur
		  | time     | bar     | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
		  | 03:00:00 | StandBy |                        |                      |                        |                      |
		  | 03:00:30 |         | 1                      | 30                   | 0                      | 0                    |
		  | 03:00:42 | Running |                        |                      |                        |                      |
		  | 03:00:53 | StandBy |                        |                      |                        |                      |
		When the application restarts at 03:00:55
		Then the following publications occur
		  | time     | bar | start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
		  | 03:01:00 |     | 02:59:00 | 03:01:00 | 1                      | 42                   | 1                      | 53-42                |
		  | 03:01:30 |     | 02:59:30 | 03:01:30 | 1                      | 42                   | 1                      | 53-42                |
		  | 03:02:00 |     | 03:00:00 | 03:02:00 | 1                      | 42                   | 1                      | 53-42                |
		  | 03:02:30 |     | 03:00:30 | 03:02:30 | 1                      | 42-30                | 1                      | 53-42                |
		  | 03:03:00 |     | 03:01:00 | 03:03:00 | 0                      | 0                    | 0                      | 0                    |

	Scenario: After restart, breaking trigger occurs before any publication
		Given a bar property of StandBy/Running enum type
		And a foo metric defined as
		  | metric                                                                           |
		  | Metric(foo).Is.Every(30).Seconds.OnAWindowOf(120).Seconds.StateMonitoringOf(bar) |
		When the application starts at 03:00:00
		Then the following publications occur
		  | time     | bar     | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
		  | 03:00:00 | StandBy |                        |                      |                        |                      |
		  | 03:00:30 |         | 1                      | 30                   | 0                      | 0                    |
		  | 03:00:42 | Running |                        |                      |                        |                      |
		  | 03:00:53 | StandBy |                        |                      |                        |                      |
		When the application restarts at 03:00:55
		Then the following publications occur
		  | time     | bar     | start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
		  | 03:00:57 | StandBy |          |          |                        |                      |                        |                      |
		  | 03:01:00 |         | 02:59:00 | 03:01:00 | 2                      | 3+42                 | 1                      | 53-42                |
		  | 03:01:30 |         | 02:59:30 | 03:01:30 | 2                      | 30+3+42              | 1                      | 53-42                |
		  | 03:02:00 |         | 03:00:00 | 03:02:00 | 2                      | 30+30+3+42           | 1                      | 53-42                |
		  | 03:02:30 |         | 03:00:30 | 03:02:30 | 2                      | 30+30+30+3+42-30     | 1                      | 53-42                |
		  | 03:03:00 |         | 03:01:00 | 03:03:00 | 1                      | 120                  | 0                      | 0                    |

	Scenario: After restart, continuous trigger occurs before any publication
		Given a bar property of StandBy/Running enum type
		And a foo metric defined as
		  | metric                                                                           |
		  | Metric(foo).Is.Every(30).Seconds.OnAWindowOf(120).Seconds.StateMonitoringOf(bar) |
		When the application starts at 03:00:00
		Then the following publications occur
		  | time     | bar     | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
		  | 03:00:00 | StandBy |                        |                      |                        |                      |
		  | 03:00:30 |         | 1                      | 30                   | 0                      | 0                    |
		  | 03:00:42 | Running |                        |                      |                        |                      |
		  | 03:00:53 | StandBy |                        |                      |                        |                      |
		When the application restarts at 03:00:55
		Then the following publications occur
		  | time     | bar     | start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
		  | 03:00:57 | Running |          |          |                        |                      |                        |                      |
		  | 03:01:00 |         | 02:59:00 | 03:01:00 | 1                      | 42                   | 2                      | 3+53-42              |
		  | 03:01:30 |         | 02:59:30 | 03:01:30 | 1                      | 42                   | 2                      | 30+3+53-42           |
		  | 03:02:00 |         | 03:00:00 | 03:02:00 | 1                      | 42                   | 2                      | 30+30+3+53-42        |
		  | 03:02:30 |         | 03:00:30 | 03:02:30 | 1                      | 42-30                | 2                      | 30+30+30+3+53-42     |
		  | 03:03:00 |         | 03:01:00 | 03:03:00 | 0                      | 0                    | 1                      | 120                  |

	Scenario: After restart, breaking trigger occurs after publications but in same window of publications before restart
		Given a bar property of StandBy/Running enum type
		And a foo metric defined as
		  | metric                                                                           |
		  | Metric(foo).Is.Every(30).Seconds.OnAWindowOf(120).Seconds.StateMonitoringOf(bar) |
		When the application starts at 03:00:00
		Then the following publications occur
		  | time     | bar     | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
		  | 03:00:00 | StandBy |                        |                      |                        |                      |
		  | 03:00:30 |         | 1                      | 30                   | 0                      | 0                    |
		  | 03:00:42 | Running |                        |                      |                        |                      |
		  | 03:00:53 | StandBy |                        |                      |                        |                      |
		When the application restarts at 03:00:55
		Then the following publications occur
		  | time     | bar     | start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
		  | 03:01:00 |         | 02:59:00 | 03:01:00 | 1                      | 42                   | 1                      | 53-42                |
		  | 03:01:30 |         | 02:59:30 | 03:01:30 | 1                      | 42                   | 1                      | 53-42                |
		  | 03:01:44 | StandBy |          |          |                        |                      |                        |                      |
		  | 03:02:00 |         | 03:00:00 | 03:02:00 | 2                      | 60-44+42             | 1                      | 53-42                |
		  | 03:02:30 |         | 03:00:30 | 03:02:30 | 2                      | 30+60-44+42-30       | 1                      | 53-42                |
		  | 03:03:00 |         | 03:01:00 | 03:03:00 | 1                      | 30+30+60-44          | 0                      | 0                    |
		  | 03:03:30 |         | 03:01:30 | 03:03:30 | 1                      | 30+30+30+60-44       | 0                      | 0                    |
		  | 03:04:00 |         | 03:02:00 | 03:04:00 | 1                      | 120                  | 0                      | 0                    |

	Scenario: After restart, continuous trigger occurs after publications but in same window of publications before restart
		Given a bar property of StandBy/Running enum type
		And a foo metric defined as
		  | metric                                                                           |
		  | Metric(foo).Is.Every(30).Seconds.OnAWindowOf(120).Seconds.StateMonitoringOf(bar) |
		When the application starts at 03:00:00
		Then the following publications occur
		  | time     | bar     | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
		  | 03:00:00 | StandBy |                        |                      |                        |                      |
		  | 03:00:30 |         | 1                      | 30                   | 0                      | 0                    |
		  | 03:00:42 | Running |                        |                      |                        |                      |
		  | 03:00:53 | StandBy |                        |                      |                        |                      |
		When the application restarts at 03:00:55
		Then the following publications occur
		  | time     | bar     | start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
		  | 03:01:00 |         | 02:59:00 | 03:01:00 | 1                      | 42                   | 1                      | 53-42                |
		  | 03:01:30 |         | 02:59:30 | 03:01:30 | 1                      | 42                   | 1                      | 53-42                |
		  | 03:01:44 | Running |          |          |                        |                      |                        |                      |
		  | 03:02:00 |         | 03:00:00 | 03:02:00 | 1                      | 42                   | 2                      | 60-44+53-42          |
		  | 03:02:30 |         | 03:00:30 | 03:02:30 | 1                      | 42-30                | 2                      | 30+60-44+53-42       |
		  | 03:03:00 |         | 03:01:00 | 03:03:00 | 0                      | 0                    | 1                      | 30+30+60-44          |
		  | 03:03:30 |         | 03:01:30 | 03:03:30 | 0                      | 0                    | 1                      | 30+30+30+60-44       |
		  | 03:04:00 |         | 03:02:00 | 03:04:00 | 0                      | 0                    | 1                      | 120                  |

	Scenario: Restart with missed publications but some previously published items are still relevant
		Given a bar property of StandBy/Running enum type
		And a foo metric defined as
		  | metric                                                                           |
		  | Metric(foo).Is.Every(30).Seconds.OnAWindowOf(120).Seconds.StateMonitoringOf(bar) |
		When the application starts at 03:00:00
		Then the following publications occur
		  | time     | bar     | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
		  | 03:00:00 | StandBy |                        |                      |                        |                      |
		  | 03:00:30 |         | 1                      | 30                   | 0                      | 0                    |
		  | 03:00:42 | Running |                        |                      |                        |                      |
		  | 03:00:53 | StandBy |                        |                      |                        |                      |
		  | 03:01:00 |         | 2                      | 60-53+42-30+30       | 1                      | 53-42                |
		  | 03:01:30 |         | 2                      | 30+60-53+42-30+30    | 1                      | 53-42                |
		When the application restarts at 03:02:17
		Then the following publications occur
		  | time     | bar | start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
		  | 03:02:30 |     | 03:00:30 | 03:02:30 | 2                      | 30+60-53+42-30       | 1                      | 53-42                |
		  | 03:03:00 |     | 03:01:00 | 03:03:00 | 1                      | 30                   | 0                      | 0                    |
		  | 03:03:30 |     | 03:01:30 | 03:03:30 | 0                      | 0                    | 0                      | 0                    |

	Scenario: Restart with missed publications but some previously published and unpublished items are still relevant
		Given a bar property of StandBy/Running enum type
		And a foo metric defined as
		  | metric                                                                           |
		  | Metric(foo).Is.Every(30).Seconds.OnAWindowOf(120).Seconds.StateMonitoringOf(bar) |
		When the application starts at 03:00:00
		Then the following publications occur
		  | time     | bar     | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
		  | 03:00:00 | StandBy |                        |                      |                        |                      |
		  | 03:00:30 |         | 1                      | 30                   | 0                      | 0                    |
		  | 03:00:42 | Running |                        |                      |                        |                      |
		  | 03:00:53 | StandBy |                        |                      |                        |                      |
		When the application restarts at 03:01:47
		Then the following publications occur
		  | time     | bar | start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
		  | 03:02:00 |     | 03:00:00 | 03:02:00 | 1                      | 42                   | 1                      | 53-42                |
		  | 03:02:30 |     | 03:00:30 | 03:02:30 | 1                      | 42-30                | 1                      | 53-42                |
		  | 03:03:00 |     | 03:01:00 | 03:03:00 | 0                      | 0                    | 0                      | 0                    |

	Scenario: Restart with missed publications but some previously published and multiple unpublished items are still relevant
		Given a bar property of StandBy/Running enum type
		And a foo metric defined as
		  | metric                                                                           |
		  | Metric(foo).Is.Every(30).Seconds.OnAWindowOf(120).Seconds.StateMonitoringOf(bar) |
		When the application starts at 03:00:00
		Then the following publications occur
		  | time     | bar     | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
		  | 03:00:00 | StandBy |                        |                      |                        |                      |
		  | 03:00:30 |         | 1                      | 30                   | 0                      | 0                    |
		  | 03:00:42 | Running |                        |                      |                        |                      |
		  | 03:00:53 | StandBy |                        |                      |                        |                      |
		  | 03:00:58 | Running |                        |                      |                        |                      |
		When the application restarts at 03:01:47
		Then the following publications occur
		  | time     | bar | start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
		  | 03:02:00 |     | 03:00:00 | 03:02:00 | 2                      | 58-53+42             | 1                      | 53-42                |
		  | 03:02:30 |     | 03:00:30 | 03:02:30 | 2                      | 58-53+42-30          | 1                      | 53-42                |
		  | 03:03:00 |     | 03:01:00 | 03:03:00 | 0                      | 0                    | 0                      | 0                    |
