Feature: State Monitoring metrics publish values without restart

  Scenario: Publish occurence and duration after remaining in a single state for a whole period
    Given a bar property of StandBy/Running enum type
    And a foo metric defined as
      | metric                                                 |
      | Metric(foo).Is.Every(5).Seconds.StateMonitoringOf(bar) |
    When the application starts at 03:00:00
    Then the following publications occur
      | time     | bar     | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
      | 03:00:00 | StandBy |                        |                      |                        |                      |
      | 03:00:05 |         | 1                      | 5                    | 0                      | 0                    |
      | 03:00:10 |         | 1                      | 5                    | 0                      | 0                    |
      | 03:00:15 |         | 1                      | 5                    | 0                      | 0                    |

  Scenario: Publish occurence and duration after remaining in a single state for a another whole period
    Given a bar property of StandBy/Running enum type
    And a foo metric defined as
      | metric                                                  |
      | Metric(foo).Is.Every(30).Seconds.StateMonitoringOf(bar) |
    When the application starts at 03:00:00
    Then the following publications occur
      | time     | bar     | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
      | 03:00:00 | Running |                        |                      |                        |                      |
      | 03:00:30 |         | 0                      | 0                    | 1                      | 30                   |
      | 03:01:00 |         | 0                      | 0                    | 1                      | 30                   |
      | 03:01:30 |         | 0                      | 0                    | 1                      | 30                   |

  Scenario: Publish occurence and duration after state change
    Given a bar property of StandBy/Running enum type
    And a foo metric defined as
      | metric                                                  |
      | Metric(foo).Is.Every(30).Seconds.StateMonitoringOf(bar) |
    When the application starts at 03:00:00
    Then the following publications occur
      | time     | bar     | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
      | 03:00:00 | StandBy |                        |                      |                        |                      |
      | 03:00:30 |         | 1                      | 30                   | 0                      | 0                    |
      | 03:00:42 | Running |                        |                      |                        |                      |
      | 03:01:00 |         | 1                      | 12                   | 1                      | 18                   |
      | 03:01:30 |         | 0                      | 0                    | 1                      | 30                   |

  Scenario: Publish occurence and duration after two state changes in same period
    Given a bar property of StandBy/Running enum type
    And a foo metric defined as
      | metric                                                  |
      | Metric(foo).Is.Every(30).Seconds.StateMonitoringOf(bar) |
    When the application starts at 03:00:00
    Then the following publications occur
      | time     | bar     | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
      | 03:00:00 | StandBy |                        |                      |                        |                      |
      | 03:00:30 |         | 1                      | 30                   | 0                      | 0                    |
      | 03:00:42 | Running |                        |                      |                        |                      |
      | 03:00:53 | StandBy |                        |                      |                        |                      |
      | 03:01:00 |         | 2                      | 12+7                 | 1                      | 11                   |
      | 03:01:30 |         | 1                      | 30                   | 0                      | 0                    |

  Scenario: Publish occurence and duration after three state changes in same period
    Given a bar property of StandBy/Running enum type
    And a foo metric defined as
      | metric                                                  |
      | Metric(foo).Is.Every(30).Seconds.StateMonitoringOf(bar) |
    When the application starts at 03:00:00
    Then the following publications occur
      | time     | bar     | start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
      | 03:00:00 | StandBy |          |          |                        |                      |                        |                      |
      | 03:00:30 |         | 03:00:00 | 03:00:30 | 1                      | 30                   | 0                      | 0                    |
      | 03:00:42 | Running |          |          |                        |                      |                        |                      |
      | 03:00:53 | StandBy |          |          |                        |                      |                        |                      |
      | 03:00:58 | Running |          |          |                        |                      |                        |                      |
      | 03:01:00 |         | 03:00:30 | 03:01:00 | 2                      | 12+5                 | 2                      | 11+2                 |
      | 03:01:30 |         | 03:01:00 | 03:01:30 | 0                      | 0                    | 1                      | 30                   |
      | 03:02:00 |         | 03:01:30 | 03:02:00 | 0                      | 0                    | 1                      | 30                   |

  Scenario: Publish occurence and duration after changes in multiple periods
    Given a bar property of StandBy/Running enum type
    And a foo metric defined as
      | metric                                                  |
      | Metric(foo).Is.Every(30).Seconds.StateMonitoringOf(bar) |
    When the application starts at 03:00:00
    Then the following publications occur
      | time     | bar     | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
      | 03:00:00 | StandBy |                        |                      |                        |                      |
      | 03:00:30 |         | 1                      | 30                   | 0                      | 0                    |
      | 03:00:42 | Running |                        |                      |                        |                      |
      | 03:00:53 | StandBy |                        |                      |                        |                      |
      | 03:00:58 | Running |                        |                      |                        |                      |
      | 03:01:00 |         | 2                      | 12+5                 | 2                      | 11+2                 |
      | 03:01:07 | StandBy |                        |                      |                        |                      |
      | 03:01:14 | Running |                        |                      |                        |                      |
      | 03:01:30 |         | 1                      | 14-7                 | 2                      | 30-14+7              |
      | 03:01:42 | StandBy |                        |                      |                        |                      |
      | 03:02:00 |         | 1                      | 60-42                | 1                      | 42-30                |

  Scenario: Publish occurence and duration after start inside a period
    Given a bar property of StandBy/Running enum type
    And a foo metric defined as
      | metric                                                  |
      | Metric(foo).Is.Every(30).Seconds.StateMonitoringOf(bar) |
    When the application starts at 03:00:13
    Then the following publications occur
      | time     | bar     | start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
      | 03:00:18 | StandBy |          |          |                        |                      |                        |                      |
      | 03:00:30 |         | 03:00:00 | 03:00:30 | 1                      | 30-18                | 0                      | 0                    |
      | 03:00:42 | Running |          |          |                        |                      |                        |                      |
      | 03:00:53 | StandBy |          |          |                        |                      |                        |                      |
      | 03:00:58 | Running |          |          |                        |                      |                        |                      |
      | 03:01:00 |         | 03:00:30 | 03:01:00 | 2                      | (42-30)+(58-53)      | 2                      | (53-42)+(60-58)      |
      | 03:01:30 |         | 03:01:00 | 03:01:30 | 0                      | 0                    | 1                      | 30                   |
      | 03:02:00 |         | 03:01:30 | 03:02:00 | 0                      | 0                    | 1                      | 30                   |

  Scenario: Publish occurence and duration before receiving any state
    Given a bar property of StandBy/Running enum type
    And a foo metric defined as
      | metric                                                  |
      | Metric(foo).Is.Every(30).Seconds.StateMonitoringOf(bar) |
    When the application starts at 03:00:13
    Then the following publications occur
      | time     | bar     | start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
      | 03:00:30 |         | 03:00:00 | 03:00:30 | 0                      | 0                    | 0                      | 0                    |
      | 03:01:00 |         | 03:00:30 | 03:01:00 | 0                      | 0                    | 0                      | 0                    |
      | 03:01:17 | Running |          |          |                        |                      |                        |                      |
      | 03:01:30 |         | 03:01:00 | 03:01:30 | 0                      | 0                    | 1                      | 30-17                |
      | 03:02:00 |         | 03:01:30 | 03:02:00 | 0                      | 0                    | 1                      | 30                   |
