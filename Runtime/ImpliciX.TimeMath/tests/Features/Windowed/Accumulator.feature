#language: en

Feature: Windowed Accumulator publish values regularly

The accumulator is used to give the values in a windowed time period

  Background:
    Given a TimeMath service start at 1 minutes
    And the "heat" service primary period is define to 2 minutes
    And the service has a window period of 4 minutes
    And a "temperature" Accumulator Computer

  Scenario: Accumulator should publish Accumulator computer when value is empty
    Given the service is started
    When the time now 2
    Then I get 1 TimeMaths Event
    And the "heat:samples_count" TimeMaths Event value should be 0
    And the "heat:accumulated_value" TimeMaths Event value should be 0
    When the time now 4
    Then I get 1 TimeMaths Event
    And the "heat:samples_count" TimeMaths Event value should be 0
    And the "heat:accumulated_value" TimeMaths Event value should be 0

  Scenario: Accumulator should also send an window event at the end of the period
    Given the service is started
    And these "temperature" are received:
      | minute | temperature |
      | 1      | 1.0         |
      | 2      | 5.0         |
    When the time now 2
    Then I get 1 TimeMaths Event
    And the "heat:accumulated_value" TimeMaths Event value should be 6
    And the "heat:samples_count" TimeMaths Event value should be 2
    And these "temperature" are received:
      | minute | temperature |
      | 3      | 1.0         |
      | 4      | 1.0         |
    When the time now 4
    Then I get 1 TimeMaths Event
    And the "heat:accumulated_value" TimeMaths Event value should be 8
    And the "heat:samples_count" TimeMaths Event value should be 4
    When the time now 6
    Then I get 1 TimeMaths Event
    And the "heat:accumulated_value" TimeMaths Event value should be 2
    And the "heat:samples_count" TimeMaths Event value should be 2

  Scenario: Accumulator should send an event at the end of the Period
    Given the service is started
    Then these events occurs before system tick:
      | minute | temperature | heat:samples_count | heat:accumulated_value | heat.Start | heat.End |
      | 0      | 10.0        |                    |                        |            |          |
      | 1      | 15.0        |                    |                        |            |          |
      | 2      | 13.0        | 3                  | 38.0                   | 1.0        | 2.0      |
      | 3      | 20.0        |                    |                        |            |          |
      | 4      | 30.0        | 5                  | 88.0                   | 1.0        | 4.0      |
      | 5      | 30.0        |                    |                        |            |          |
      | 6      | 10.0        | 4                  | 90.0                   | 2.0        | 6.0      |
      | 7      |             |                    |                        |            |          |
      | 8      | 32.0        | 3                  | 72.0                   | 4.0        | 8.0      |

  Scenario: Accumulator should not publish events with the right start and end after restart
    When the service is started
    Then these events occurs before system tick:
      | minute | temperature | heat:accumulated_value | heat.Start | heat.End |
      | 1      | 10.0        |                        |            |          |
      | 2      | 15.0        | 25.0                   | 1.0        | 2.0      |
      | 3      | 13.0        |                        |            |          |
      | 4      | 20.0        | 58.0                   | 1.0        | 4.0      |
      | 5      | 30.0        |                        |            |          |
      | 6      | 30.0        | 93.0                   | 2.0        | 6.0      |
    When the application is restarted at 7
    Then I get 0 TimeMaths Event
    Then these events occurs before system tick:
      | minute | temperature | heat:accumulated_value | heat.Start | heat.End |
      | 8      | 32.0        | 92.0                   | 4.0        | 8.0      |

  Scenario: Accumulator should publish events with the right start and end after restart
    When the service is started
    Then these events occurs before system tick:
      | minute | temperature | heat:accumulated_value | heat.Start | heat.End |
      | 1      | 10.0        |                        |            |          |
      | 2      | 15.0        | 25.0                   | 1.0        | 2.0      |
      | 3      | 13.0        |                        |            |          |
      | 4      | 20.0        | 58.0                   | 1.0        | 4.0      |
      | 5      | 30.0        |                        |            |          |
    When the application is restarted at 7
    Then these events occurs before system tick:
      | minute | temperature | heat:accumulated_value | heat.Start | heat.End |
      | 7      |             | 63.0                   | 2.0        | 5.0      |
      | 8      | 32.0        | 62.0                   | 4.0        | 8.0      |
