Feature: State Monitoring metrics publish values without restart

  Scenario: Publish occurence and duration after remaining in a single state for a whole period
    Given a bar property of StandBy/Running enum type
    And a foo metric defined as
      | metric                                                                         |
      | Metric(foo).Is.Every(5).Seconds.OnAWindowOf(20).Seconds.StateMonitoringOf(bar) |
    When the application starts at 03:00:00
    Then the following publications occur
      | time     | bar     | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
      | 03:00:00 | StandBy |                        |                      |                        |                      |
      | 03:00:05 |         | 1                      | 5                    | 0                      | 0                    |
      | 03:00:10 |         | 1                      | 10                   | 0                      | 0                    |
      | 03:00:15 |         | 1                      | 15                   | 0                      | 0                    |
      | 03:00:20 |         | 1                      | 20                   | 0                      | 0                    |
      | 03:00:25 |         | 1                      | 20                   | 0                      | 0                    |
      | 03:00:30 |         | 1                      | 20                   | 0                      | 0                    |

  Scenario: Publish occurence and duration after state change
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
      | 03:01:00 |         | 1                      | 42                   | 1                      | 60-42                |
      | 03:01:30 |         | 1                      | 42                   | 1                      | 90-42                |
      | 03:02:00 |         | 1                      | 42                   | 1                      | 120-42               |
      | 03:02:30 |         | 1                      | 42-30                | 1                      | 150-42               |
      | 03:03:00 |         | 0                      |                      | 1                      | 120                  |
      | 03:03:30 |         | 0                      |                      | 1                      | 120                  |

  Scenario: Publish occurence and duration after two state changes
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
      | 03:01:00 |         | 2                      | 30+12+7              | 1                      | 53-42                |
      | 03:01:30 |         | 2                      | 60+19                | 1                      | 11                   |
      | 03:02:00 |         | 2                      | 90+19                | 1                      | 11                   |
      | 03:02:30 |         | 2                      | 90+19                | 1                      | 11                   |
      | 03:03:00 |         | 1                      | 120                  | 0                      |                      |
      | 03:03:30 |         | 1                      | 120                  | 0                      |                      |

  Scenario: Publish occurence and duration after three state changes
    Given a bar property of StandBy/Running enum type
    And a foo metric defined as
      | metric                                                                           |
      | Metric(foo).Is.Every(30).Seconds.OnAWindowOf(120).Seconds.StateMonitoringOf(bar) |
    When the application starts at 03:00:00
    Then the following publications occur
      | time     | bar     | start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
      | 03:00:00 | StandBy |          |          |                        |                      |                        |                      |
      | 03:00:30 |         | 02:58:30 | 03:00:30 | 1                      | 30                   | 0                      | 0                    |
      | 03:00:42 | Running |          |          |                        |                      |                        |                      |
      | 03:00:53 | StandBy |          |          |                        |                      |                        |                      |
      | 03:00:58 | Running |          |          |                        |                      |                        |                      |
      | 03:01:00 |         | 02:59:00 | 03:01:00 | 2                      | 30+12+58-53          | 2                      | 53-42+60-58          |
      | 03:01:30 |         | 02:59:30 | 03:01:30 | 2                      | 30+17                | 2                      | 13+30                |
      | 03:02:00 |         | 03:00:00 | 03:02:00 | 2                      | 30+17                | 2                      | 13+60                |
      | 03:02:30 |         | 03:00:30 | 03:02:30 | 2                      | 17                   | 2                      | 13+90                |
      | 03:03:00 |         | 03:01:00 | 03:03:00 | 0                      |                      | 1                      | 120                  |
      | 03:03:30 |         | 03:01:30 | 03:03:30 | 0                      |                      | 1                      | 120                  |
      | 03:04:00 |         | 03:02:00 | 03:04:00 | 0                      |                      | 1                      | 120                  |

  Scenario: Publish occurence and duration after changes in multiple periods
    Given a bar property of StandBy/Running enum type
    And a foo metric defined as
      | metric                                                                           |
      | Metric(foo).Is.Every(30).Seconds.OnAWindowOf(120).Seconds.StateMonitoringOf(bar) |
    When the application starts at 03:00:00
    Then the following publications occur
      | time     | bar     | foo:StandBy:occurrence | foo:StandBy:duration     | foo:Running:occurrence | foo:Running:duration     |
      | 03:00:00 | StandBy |                        |                          |                        |                          |
      | 03:00:30 |         | 1                      | 30                       | 0                      | 0                        |
      | 03:00:42 | Running |                        |                          |                        |                          |
      | 03:00:53 | StandBy |                        |                          |                        |                          |
      | 03:00:58 | Running |                        |                          |                        |                          |
      | 03:01:00 |         | 2                      | 30+58-53+42-30           | 2                      | 0+60-58+53-42            |
      | 03:01:07 | StandBy |                        |                          |                        |                          |
      | 03:01:14 | Running |                        |                          |                        |                          |
      | 03:01:30 |         | 3                      | 47+14-7                  | 3                      | 13+30-14+7               |
      | 03:01:42 | StandBy |                        |                          |                        |                          |
      | 03:01:43 | Running |                        |                          |                        |                          |
      | 03:01:58 | StandBy |                        |                          |                        |                          |
      | 03:02:00 |         | 5                      | 54+60-58+43-42           | 4                      | 36+58-43+42-30           |
      | 03:02:21 | Running |                        |                          |                        |                          |
      | 03:02:30 |         | 5                      | 57-30+21                 | 5                      | 63+30-21                 |
      | 03:02:38 | StandBy |                        |                          |                        |                          |
      | 03:03:00 |         | 4                      | 48+60-38-(42-30)-(58-53) | 4                      | 72+38-30-(60-58)-(53-42) |
      | 03:03:30 |         | 3                      | 53+30-(14-7)             | 3                      | 67-7-(30-14)             |
      | 03:04:00 |         | 2                      | 76+30-(60-58)-(43-42)    | 1                      | 44-(42-30)-(58-43)       |
      | 03:04:30 |         | 1                      | 103+30-21                | 1                      | 17-(30-21)               |
      | 03:05:00 |         | 1                      | 112+30-(60-38)           | 0                      | 8-(38-30)                |
      | 03:05:30 |         | 1                      | 120                      | 0                      | 0                        |

  Scenario: Publish occurence and duration after start inside a period
    Given a bar property of StandBy/Running enum type
    And a foo metric defined as
      | metric                                                                           |
      | Metric(foo).Is.Every(30).Seconds.OnAWindowOf(120).Seconds.StateMonitoringOf(bar) |
    When the application starts at 03:00:13
    Then the following publications occur
      | time     | bar     | start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
      | 03:00:18 | StandBy |          |          |                        |                      |                        |                      |
      | 03:00:30 |         | 02:58:30 | 03:00:30 | 1                      | 30-18                | 0                      | 0                    |
      | 03:00:42 | Running |          |          |                        |                      |                        |                      |
      | 03:00:53 | StandBy |          |          |                        |                      |                        |                      |
      | 03:00:58 | Running |          |          |                        |                      |                        |                      |
      | 03:01:00 |         | 02:59:00 | 03:01:00 | 2                      | 12+(42-30)+(58-53)   | 2                      | (60-58)+(53-42)      |
      | 03:01:30 |         | 02:59:30 | 03:01:30 | 2                      | 12+17+0              | 2                      | 0+13+30              |
      | 03:02:00 |         | 03:00:00 | 03:02:00 | 2                      | 12+17+0+0            | 2                      | 0+13+30+30           |
      | 03:02:30 |         | 03:00:30 | 03:02:30 | 2                      | 17+0+0+0             | 2                      | 13+30+30+30          |
      | 03:03:00 |         | 03:01:00 | 03:03:00 | 0                      |                      | 1                      | 120                  |
      | 03:03:30 |         | 03:01:30 | 03:03:30 | 0                      |                      | 1                      | 120                  |

  Scenario: Publish occurence and duration before receiving any state
    Given a bar property of StandBy/Running enum type
    And a foo metric defined as
      | metric                                                                           |
      | Metric(foo).Is.Every(30).Seconds.OnAWindowOf(120).Seconds.StateMonitoringOf(bar) |
    When the application starts at 03:00:13
    Then the following publications occur
      | time     | bar     | start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
      | 03:00:30 |         | 02:58:30 | 03:00:30 | 0                      |                      | 0                      | 0                    |
      | 03:01:00 |         | 02:59:00 | 03:01:00 | 0                      |                      | 0                      | 0                    |
      | 03:01:17 | Running |          |          |                        |                      |                        |                      |
      | 03:01:30 |         | 02:59:30 | 03:01:30 | 0                      |                      | 1                      | 0+0+(30-17)          |
      | 03:02:00 |         | 03:00:00 | 03:02:00 | 0                      |                      | 1                      | 0+0+13+30            |
      | 03:02:30 |         | 03:00:30 | 03:02:30 | 0                      |                      | 1                      | 0+13+30+30           |
      | 03:03:00 |         | 03:01:00 | 03:03:00 | 0                      |                      | 1                      | 13+30+30+30          |
      | 03:03:30 |         | 03:01:30 | 03:03:30 | 0                      |                      | 1                      | 120                  |
      | 03:04:00 |         | 03:02:00 | 03:04:00 | 0                      |                      | 1                      | 120                  |

