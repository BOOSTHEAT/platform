#language: en

Feature: Accumulator publish values regularly

The Accumulator is used to give the values in a time period

  Background:
    Given a TimeMath service start at 1 minutes
    And the "heat" service primary period is define to 3 minutes
    Given a "temperature" Accumulator Computer

  Scenario: Accumulator should publish Accumulator computer when value is empty
    Given the service is started
    When the time now 3
    Then I get 1 TimeMaths Event
    And the "heat:samples_count" TimeMaths Event value should be 0
    And the "heat:accumulated_value" TimeMaths Event value should be 0

  Scenario: A TimeMath in Accumulator should send an event at the end of the period
    Given the service is started
    And these "temperature" are received:
      | minute | temperature |
      | 1      | 1.0         |
      | 2      | 1.0         |
      | 3      | 5.0         |
    When the time now 3
    Then I get 1 TimeMaths Event
    And the "heat:samples_count" TimeMaths Event value should be 3
    And the "heat:accumulated_value" TimeMaths Event value should be 7

  Scenario: A TimeMath in Accumulator should send an event with already received values at the end of the period
    Given the service is started
    And these "temperature" are received:
      | minute | temperature |
      | 0      | 1.0         |
      | 1      | 1.0         |
      | 2      | 5.0         |
      | 3      | 15.0        |
    When the time now 3
    Then I get 1 TimeMaths Event
    And the "heat:samples_count" TimeMaths Event value should be 4
    And the "heat:accumulated_value" TimeMaths Event value should be 22

  Scenario: A TimeMath in Accumulator should not publish the event when receiving values at the end of the period
  after publishing
    Given the service is started
    And these "temperature" are received:
      | minute | temperature |
      | 0      | 1.0         |
      | 1      | 1.0         |
      | 2      | 5.0         |
    And the time now 3
    And these "temperature" are received:
      | minute | temperature |
      | 3      | 15.0        |
    Then I get 0 TimeMaths Event

  Scenario: A TimeMath in Accumulator should not send event after the end of the period
    Given the service is started
    And these "temperature" are received:
      | minute | temperature |
      | 0      | 1.0         |
      | 1      | 1.0         |
      | 2      | 5.0         |
    When the time now 4
    Then I get 0 TimeMaths Event

  Scenario: Accumulator should keep values before publish
    Given the service is started
    When the time now 3
    Then I get 1 TimeMaths Event
    And these "temperature" are received:
      | minute | temperature |
      | 4      | 1.0         |
      | 5      | 1.0         |
      | 6      | 5.0         |
    When the time now 6
    Then I get 1 TimeMaths Event
    And the "heat:samples_count" TimeMaths Event value should be 3
    And the "heat:accumulated_value" TimeMaths Event value should be 7

  Scenario: Accumulator should also publish events if no new values arrives
    Given the service is started
    And these "temperature" are received:
      | minute | temperature |
      | 0      | 1.0         |
      | 1      | 1.0         |
      | 2      | 5.0         |
    When the time now 3
    Then I get 1 TimeMaths Event
    And the "heat:samples_count" TimeMaths Event value should be 3
    And the "heat:accumulated_value" TimeMaths Event value should be 7
    When the time now 6
    Then I get 1 TimeMaths Event

  Scenario: Accumulator should publish last measure events after restart
  the unpublished values should still be known by the system on restart
    Given the service is started
    And these "temperature" are received:
      | minute | temperature |
      | 0      | 1.0         |
      | 1      | 1.0         |
      | 2      | 5.0         |
    When the application is restarted at 2
    And the time now 3
    Then I get 1 TimeMaths Event
    And the "heat:samples_count" TimeMaths Event value should be 3
    And the "heat:accumulated_value" TimeMaths Event value should be 7

  Scenario: Accumulator should publish unpublished events after restart
  the unpublished values should still be known by the system on restart
    Given the service is started
    And these "temperature" are received:
      | minute | temperature |
      | 2      | 2.0         |
      | 3      | 1.0         |
    And the time now 3
    And these "temperature" are received:
      | minute | temperature |
      | 4      | 1.0         |
      | 5      | 5.0         |
    When the application is restarted at 7
    Then I get 1 TimeMaths Event
    And the "heat:samples_count" TimeMaths Event value should be 2
    And the "heat:accumulated_value" TimeMaths Event value should be 6

  Scenario: Accumulator should not publish unpublished events after restart before publish time
  the unpublished values should still be known by the system on restart
    Given the service is started
    And these "temperature" are received:
      | minute | temperature |
      | 2      | 2.0         |
      | 3      | 1.0         |
    And the time now 3
    And these "temperature" are received:
      | minute | temperature |
      | 4      | 1.0         |
    When the application is restarted at 5
    Then I get 0 TimeMaths Event

  Scenario: Accumulator should not publish published events after restart
  the unpublished values should still be known by the system on restart
    Given the service is started
    And these "temperature" are received:
      | minute | temperature |
      | 0      | 1.0         |
      | 1      | 1.0         |
      | 2      | 5.0         |
    And the time now 3
    Then I get 1 TimeMaths Event
    Given the time now 3
    When the application is restarted
    Then I get 0 TimeMaths Event

  Scenario: A TimeMath in Accumulator should send an event at the end of each period
    Given the service is started
    And these "temperature" are received:
      | minute | temperature |
      | 0      | 1.0         |
      | 1      | 1.0         |
      | 2      | 5.0         |
      | 3      | 1.0         |
    When the time now 3
    Then I get 1 TimeMaths Event
    And the "heat:samples_count" TimeMaths Event value should be 4
    And the "heat:accumulated_value" TimeMaths Event value should be 8
    And these "temperature" are received:
      | minute | temperature |
      | 4      | 1.0         |
      | 5      | 5.0         |
      | 6      | 1.0         |
    When the time now 6
    Then I get 1 TimeMaths Event
    And the "heat:samples_count" TimeMaths Event value should be 3
    And the "heat:accumulated_value" TimeMaths Event value should be 7

  @Ignore
  Scenario: Accumulator with groups should publish events for every groups
    Given a group every 4 minutes
    And a group every 5 minutes
    When the service is started
    Then these events occurs before system tick:
      | minute | temperature | heat:accumulated_value | heat_4Minutes | heat_5Minutes |
      | 1      | 10.0        |                        |               |               |
      | 2      | 15.0        |                        |               |               |
      | 3      | 13.0        | 38.0                   |               |               |
      | 4      | 20.0        |                        | 58.0          |               |
      | 5      | 30.0        |                        |               | 88.0          |
      | 6      | 35.0        | 85.0                   |               |               |
      | 7      | 7.0         |                        |               |               |
      | 8      | 32.0        |                        | 99.0          |               |
      | 9      | 38.0        | 77.0                   |               |               |
      | 10     |             |                        |               | 112.0         |
      | 11     | 40.0        |                        |               |               |
      | 12     |             | 40.0                   | 78.0          |               |
      | 14     | 31.0        |                        |               |               |
      | 15     |             | 31.0                   |               | 71.0          |

  @Ignore
  Scenario: Accumulator with groups should publish previous events for every groups if received after tick
    Given a group every 4 minutes
    And a group every 5 minutes
    When the service is started
    Then these events occurs after system tick:
      | minute | temperature | heat:accumulated_value | heat_4Minutes | heat_5Minutes |
      | 1      | 10.0        |                        |               |               |
      | 2      | 15.0        |                        |               |               |
      | 3      | 13.0        | 25.0                   |               |               |
      | 4      | 20.0        |                        | 38.0          |               |
      | 5      | 30.0        |                        |               | 58.0          |
      | 6      | 35.0        | 66.0                   |               |               |
      | 7      | 7.0         |                        |               |               |
      | 8      | 32.0        |                        | 92.0          |               |
      | 9      | 38.0        | 74.0                   |               |               |
      | 10     |             |                        |               | 142.0         |
      | 11     | 40.0        |                        |               |               |
      | 12     |             | 78.0                   | 110.0         |               |
      | 14     | 31.0        |                        |               |               |
      | 15     |             | 31.0                   |               | 71.0          |

  Scenario: Accumulator should publish events with the right start and end
    When the service is started
    Then these events occurs before system tick:
      | minute | temperature | heat:samples_count | heat:accumulated_value | heat.Start | heat.End |
      | 1      | 10.0        |                    |                        |            |          |
      | 2      | 15.0        |                    |                        |            |          |
      | 3      | 13.0        | 3                  | 38.0                   | 1.0        | 3.0      |
      | 4      | 20.0        |                    |                        |            |          |
      | 5      | 30.0        |                    |                        |            |          |
      | 6      |             | 2                  | 50.0                   | 3.0        | 6.0      |
      | 8      | 32.0        |                    |                        |            |          |
      | 9      |             | 1                  | 32.0                   | 7.0        | 9.0      |
      | 10     | 38.0        |                    |                        |            |          |
      | 11     | 40.0        |                    |                        |            |          |
      | 12     |             | 2                  | 78.0                   | 9.0        | 12.0     |
      | 14     | 31.0        |                    |                        |            |          |
      | 15     |             | 1                  | 31.0                   | 12.0       | 15.0     |
      | 18     |             | 0                  | 0.0                    | 15.0       | 18.0     |
      | 19     | 31.0        |                    |                        |            |          |
      | 21     |             | 1                  | 31.0                   | 18.0       | 21.0     |
  #    When the application is restarted at 22
  #    Then these events occurs before system tick:
  #      | minute | temperature | heat:samples_count | heat:accumulated_value | heat.Start | heat.End |
  #      | 23     | 10          |                    |                        |            |          |
  #      | 24     |             | 1                  | 10.0                   | 18.0       | 24.0     |

  Scenario: Accumulator should not publish events with the right start and end after restart
    When the service is started
    Then these events occurs before system tick:
      | minute | temperature | heat:accumulated_value | heat.Start | heat.End |
      | 1      | 10.0        |                        |            |          |
      | 2      | 15.0        |                        |            |          |
      | 3      | 13.0        | 38.0                   | 1.0        | 3.0      |
      | 4      | 20.0        |                        |            |          |
    When the application is restarted at 5
    Then I get 0 TimeMaths Event
    Then these events occurs before system tick:
      | minute | temperature | heat:accumulated_value | heat.Start | heat.End |
      | 6      |             | 20.0                   | 3.0        | 6.0      |
      | 8      | 32.0        |                        |            |          |
      | 9      |             | 32.0                   | 6.0        | 9.0      |

  Scenario: Accumulator should publish events with the right start and end after restart
    When the service is started
    Then these events occurs before system tick:
      | minute | temperature | heat:accumulated_value | heat.Start | heat.End |
      | 1      | 10.0        |                        |            |          |
      | 2      | 15.0        |                        |            |          |
      | 3      | 13.0        | 38.0                   | 1.0        | 3.0      |
      | 4      | 20.0        |                        |            |          |
      | 5      | 30.0        |                        |            |          |
    When the application is restarted at 7
    Then these events occurs before system tick:
      | minute | temperature | heat:accumulated_value | heat.Start | heat.End |
      | 7      |             | 50.0                   | 3.0        | 5.0      |
      | 8      | 32.0        |                        |            |          |
      | 9      |             | 32.0                   | 7.0        | 9.0      |
      | 10     | 38.0        |                        |            |          |
      | 11     | 40.0        |                        |            |          |
      | 12     |             | 78.0                   | 9.0        | 12.0     |
      | 14     | 31.0        |                        |            |          |
      | 15     |             | 31.0                   | 12.0       | 15.0     |
