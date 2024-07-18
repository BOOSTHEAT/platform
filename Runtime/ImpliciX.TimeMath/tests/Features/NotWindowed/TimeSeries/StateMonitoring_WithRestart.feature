Feature: State Monitoring metrics publish values with restart

  Scenario: Publish partial single known occurence and duration at restart
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
    When the application restarts at 03:00:55
    Then the following publications occur
      | time     | bar | start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
      | 03:01:00 |     | 03:00:30 | 03:01:00 | 1                      | 42-30                | 0                      | 0                    |

  Scenario: After restart, single past trigger is used only for 1st publication
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
    When the application restarts at 03:00:55
    Then the following publications occur
      | time     | bar     |start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
      | 03:01:00 |         |03:00:30 | 03:01:00 | 1                      | 42-30                | 0                      | 0                    |
      | 03:01:30 |         |03:01:00 | 03:01:30 | 0                      | 0                    | 0                      | 0                    |
      | 03:02:00 |         |03:01:30 | 03:02:00 | 0                      | 0                    | 0                      | 0                    |
      | 03:02:12 | StandBy |         |          |                        |                      |                        |                      |
      | 03:02:30 |         |03:02:00 | 03:02:30 | 1                      | 30-12                | 0                      | 0                    |

  Scenario: Publish full single known occurence and duration at restart
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
    When the application restarts at 03:00:55
    Then the following publications occur
      | time     | bar     |start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
      | 03:01:00 |         |03:00:30 | 03:01:00 | 1                      | 42-30                | 1                      | 53-42                |

  Scenario: After restart, multiple past triggers are used only for 1st publication
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
    When the application restarts at 03:00:55
    Then the following publications occur
      | time     | bar     |start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
      | 03:01:00 |         |03:00:30 | 03:01:00 | 1                      | 42-30                | 1                      | 53-42                |
      | 03:01:30 |         |03:01:00 | 03:01:30 | 0                      | 0                    | 0                      | 0                    |
      | 03:02:00 |         |03:01:30 | 03:02:00 | 0                      | 0                    | 0                      | 0                    |
      | 03:02:12 | StandBy |         |          |                        |                      |                        |                      |
      | 03:02:30 |         |03:02:00 | 03:02:30 | 1                      | 30-12                | 0                      | 0                    |

  Scenario: Continue same partial single known occurence and duration at restart
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
    When the application restarts at 03:00:55
    Then the following publications occur
      | time     | bar     | start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
      | 03:00:56 | StandBy |          |          |                        |                      |                        |                      |
      | 03:01:00 |         | 03:00:30 | 03:01:00 | 2                      | 60-56+42-30          | 0                      | 0                    |

  Scenario: After restart, continuation on same partial past is used for subsequent publications
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
    When the application restarts at 03:00:55
    Then the following publications occur
      | time     | bar     | start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
      | 03:00:56 | StandBy |          |          |                        |                      |                        |                      |
      | 03:01:00 |         | 03:00:30 | 03:01:00 | 2                      | 60-56+42-30          | 0                      | 0                    |
      | 03:01:30 |         | 03:01:00 | 03:01:30 | 1                      | 30                   | 0                      | 0                    |
      | 03:02:00 |         | 03:01:30 | 03:02:00 | 1                      | 30                   | 0                      | 0                    |
      | 03:02:12 | Running |          |          |                        |                      |                        |                      |
      | 03:02:30 |         | 03:02:00 | 03:02:30 | 1                      | 12                   | 1                      | 30-12                |

  Scenario: Continue same full single known occurence and duration at restart
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
    When the application restarts at 03:00:55
    Then the following publications occur
      | time     | bar     | start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
      | 03:00:56 | Running |          |          |                        |                      |                        |                      |
      | 03:01:00 |         | 03:00:30 | 03:01:00 | 1                      | 42-30                | 2                      | 60-56+53-42          |

  Scenario: After restart, continuation on same full past is used for subsequent publications
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
    When the application restarts at 03:00:55
    Then the following publications occur
      | time     | bar     | start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
      | 03:00:56 | Running |          |          |                        |                      |                        |                      |
      | 03:01:00 |         | 03:00:30 | 03:01:00 | 1                      | 42-30                | 2                      | 60-56+53-42          |
      | 03:01:30 |         | 03:01:00 | 03:01:30 | 0                      | 0                    | 1                      | 30                   |
      | 03:02:00 |         | 03:01:30 | 03:02:00 | 0                      | 0                    | 1                      | 30                   |
      | 03:02:12 | StandBy |          |          |                        |                      |                        |                      |
      | 03:02:30 |         | 03:02:00 | 03:02:30 | 1                      | 30-12                | 1                      | 12                   |

  Scenario: Continue other partial single known occurence and duration at restart
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
    When the application restarts at 03:00:55
    Then the following publications occur
      | time     | bar     | start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
      | 03:00:56 | Running |          |          |                        |                      |                        |                      |
      | 03:01:00 |         | 03:00:30 | 03:01:00 | 1                      | 42-30                | 1                      | 60-56                |

  Scenario: After restart, continuation on other partial past is used for subsequent publications
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
    When the application restarts at 03:00:55
    Then the following publications occur
      | time     | bar     | start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
      | 03:00:56 | Running |          |          |                        |                      |                        |                      |
      | 03:01:00 |         | 03:00:30 | 03:01:00 | 1                      | 42-30                | 1                      | 60-56                |
      | 03:01:30 |         | 03:01:00 | 03:01:30 | 0                      | 0                    | 1                      | 30                   |
      | 03:02:00 |         | 03:01:30 | 03:02:00 | 0                      | 0                    | 1                      | 30                   |
      | 03:02:12 | StandBy |          |          |                        |                      |                        |                      |
      | 03:02:30 |         | 03:02:00 | 03:02:30 | 1                      | 30-12                | 1                      | 12                   |

  Scenario: Continue other full single known occurence and duration at restart
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
    When the application restarts at 03:00:55
    Then the following publications occur
      | time     | bar     | start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
      | 03:00:56 | StandBy |          |          |                        |                      |                        |                      |
      | 03:01:00 |         | 03:00:30 | 03:01:00 | 2                      | 60-56+42-30          | 1                      | 53-42                |

  Scenario: After restart, continuation on other full past is used for subsequent publications
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
    When the application restarts at 03:00:55
    Then the following publications occur
      | time     | bar     | start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
      | 03:00:56 | StandBy |          |          |                        |                      |                        |                      |
      | 03:01:00 |         | 03:00:30 | 03:01:00 | 2                      | 60-56+42-30          | 1                      | 53-42                |
      | 03:01:30 |         | 03:01:00 | 03:01:30 | 1                      | 30                   | 0                      | 0                    |
      | 03:02:00 |         | 03:01:30 | 03:02:00 | 1                      | 30                   | 0                      | 0                    |
      | 03:02:12 | Running |          |          |                        |                      |                        |                      |
      | 03:02:30 |         | 03:02:00 | 03:02:30 | 1                      | 12                   | 1                      | 30-12                |

  Scenario: Publish old partial single known occurence and duration at restart
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
    When the application restarts at 03:01:15
    Then the following publications occur
      | time     | bar | start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
      | 03:01:15 |     | 03:00:30 | 03:01:00 | 1                      | 42-30                | 0                      | 0                    |

  Scenario: After restart, old single past trigger is used only for 1st publication
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
    When the application restarts at 03:01:15
    Then the following publications occur
      | time     | bar     |start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
      | 03:01:15 |         |03:00:30 | 03:01:00 | 1                      | 42-30                | 0                      | 0                    |
      | 03:01:30 |         |03:01:00 | 03:01:30 | 0                      | 0                    | 0                      | 0                    |
      | 03:02:00 |         |03:01:30 | 03:02:00 | 0                      | 0                    | 0                      | 0                    |
      | 03:02:12 | StandBy |         |          |                        |                      |                        |                      |
      | 03:02:30 |         |03:02:00 | 03:02:30 | 1                      | 30-12                | 0                      | 0                    |

  Scenario: Publish old full single known occurence and duration at restart
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
    When the application restarts at 03:01:15
    Then the following publications occur
      | time     | bar     |start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
      | 03:01:15 |         |03:00:30 | 03:01:00 | 1                      | 42-30                | 1                      | 53-42                |

  Scenario: After restart, old multiple past triggers are used only for 1st publication
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
    When the application restarts at 03:01:15
    Then the following publications occur
      | time     | bar     |start    | end      | foo:StandBy:occurrence | foo:StandBy:duration | foo:Running:occurrence | foo:Running:duration |
      | 03:01:15 |         |03:00:30 | 03:01:00 | 1                      | 42-30                | 1                      | 53-42                |
      | 03:01:30 |         |03:01:00 | 03:01:30 | 0                      | 0                    | 0                      | 0                    |
      | 03:02:00 |         |03:01:30 | 03:02:00 | 0                      | 0                    | 0                      | 0                    |
      | 03:02:12 | StandBy |         |          |                        |                      |                        |                      |
      | 03:02:30 |         |03:02:00 | 03:02:30 | 1                      | 30-12                | 0                      | 0                    |
