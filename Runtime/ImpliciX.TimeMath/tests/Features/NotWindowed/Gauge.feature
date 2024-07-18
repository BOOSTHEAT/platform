#language: en

Feature: Gauge publish values regularly

The gauge is used to give the values in a time period

  Background:
    Given a TimeMath service
  #start at 1 minutes
    And the "heat" service primary period is define to 3 minutes
    Given a "temperature" Gauge Computer

  Scenario: Gauge should not publish Gauge computer when value is empty
    Given the service is started
    When the time now 3
    Then I get 0 TimeMaths Event

  Scenario: A TimeMath in Gauge should send an event at the end of the period
    Given the service is started
    And these "temperature" are received:
      | minute | temperature |
      | 0      | 1.0         |
      | 1      | 1.0         |
      | 2      | 5.0         |
    When the time now 3
    Then I get 1 TimeMaths Event
    And the "heat" TimeMaths Event value should be 5

  Scenario: A TimeMath in Gauge should send an event with already received values at the end of the period
    Given the service is started
    And these "temperature" are received:
      | minute | temperature |
      | 0      | 1.0         |
      | 1      | 1.0         |
      | 2      | 5.0         |
      | 3      | 15.0        |
    When the time now 3
    Then I get 1 TimeMaths Event
    And the "heat" TimeMaths Event value should be 15

  Scenario: A TimeMath in Gauge should not publish the event when receiving values at the end of the period
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

  Scenario: A TimeMath in Gauge should not send event after the end of the period
    Given the service is started
    And these "temperature" are received:
      | minute | temperature |
      | 0      | 1.0         |
      | 1      | 1.0         |
      | 2      | 5.0         |
    When the time now 4
    Then I get 0 TimeMaths Event

  Scenario: Gauge should keep values before publish
    Given the service is started
    When the time now 3
    Then I get 0 TimeMaths Event
    And these "temperature" are received:
      | minute | temperature |
      | 4      | 1.0         |
      | 5      | 1.0         |
      | 6      | 5.0         |
    When the time now 6
    Then I get 1 TimeMaths Event
    And the "heat" TimeMaths Event value should be 5

  Scenario: Gauge should not publish events if no new values arrives
    Given the service is started
    And these "temperature" are received:
      | minute | temperature |
      | 0      | 1.0         |
      | 1      | 1.0         |
      | 2      | 5.0         |
    When the time now 3
    Then I get 1 TimeMaths Event
    And the "heat" TimeMaths Event value should be 5
    When the time now 6
    Then I get 1 TimeMaths Event
    And the "heat" TimeMaths Event value should be 5

  Scenario: Gauge should publish last measure events after restart
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
    And the "heat" TimeMaths Event value should be 5

  Scenario: Gauge should publish unpublished events after restart
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
    And the "heat" TimeMaths Event value should be 5

  Scenario: Gauge should not publish unpublished events after restart before publish time
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

  Scenario: Gauge should not publish published events after restart
  the unpublished values should still be known by the system on restart
    Given the service is started
    And these "temperature" are received:
      | minute | temperature |
      | 0      | 1.0         |
      | 1      | 1.0         |
      | 2      | 5.0         |
    And the time now 3
    Then I get 1 TimeMaths Event
    When the application is restarted at 3
    Then I get 0 TimeMaths Event

  @Ignore
  Scenario: Gauge with groups should publish events for every groups
    Given a group every 4 minutes
    And a group every 5 minutes
    When the service is started
    Then these events occurs before system tick:
      | minute | temperature | heat | heat_4Minutes | heat_5Minutes |
      | 1      | 10.0        |      |               |               |
      | 2      | 15.0        |      |               |               |
      | 3      | 13.0        | 13.0 |               |               |
      | 4      | 20.0        |      | 20.0          |               |
      | 5      | 30.0        |      |               | 30.0          |
      | 6      | 35.0        | 35.0 |               |               |
      | 7      | 7.0         |      |               |               |
      | 8      | 32.0        |      | 32.0          |               |
      | 9      | 38.0        | 38.0 |               |               |
      | 10     |             |      |               | 38.0          |
      | 11     | 40.0        |      |               |               |
      | 12     |             | 40.0 | 40.0          |               |
      | 14     | 31.0        |      |               |               |
      | 15     |             | 31.0 |               | 31.0          |

  @Ignore
  Scenario: Gauge with groups should publish previous events for every groups if received after tick
    Given a group every 4 minutes
    And a group every 5 minutes
    When the service is started
    Then these events occurs after system tick:
      | minute | temperature | heat | heat_4Minutes | heat_5Minutes |
      | 1      | 10.0        |      |               |               |
      | 2      | 15.0        |      |               |               |
      | 3      | 13.0        | 15.0 |               |               |
      | 4      | 20.0        |      | 13.0          |               |
      | 5      | 30.0        |      |               | 20.0          |
      | 6      | 35.0        | 30.0 |               |               |
      | 7      | 7.0         |      |               |               |
      | 8      | 32.0        |      | 7.0           |               |
      | 9      | 38.0        | 32.0 |               |               |
      | 10     |             |      |               | 38.0          |
      | 11     | 40.0        |      |               |               |
      | 12     |             | 40.0 | 40.0          |               |
      | 14     | 31.0        |      |               |               |
      | 15     |             | 31.0 |               | 31.0          |

  Scenario: Gauge should publish events with the right start and end
    When the service is started
    Then these events occurs before system tick:
      | minute | temperature | heat | heat.Start | heat.End |
      | 1      | 10.0        |      |            |          |
      | 2      | 15.0        |      |            |          |
      | 3      | 13.0        | 13.0 | 0.0        | 3.0      |
      | 4      | 20.0        |      |            |          |
      | 5      | 30.0        |      |            |          |
      | 6      | 35.0        | 35.0 | 3.0        | 6.0      |
      | 7      | 7.0         |      |            |          |
      | 8      | 32.0        |      |            |          |
      | 9      | 38.0        | 38.0 | 6.0        | 9.0      |
      | 10     |             |      |            |          |
      | 11     | 40.0        |      |            |          |
      | 12     |             | 40.0 | 9.0        | 12.0     |
      | 14     | 31.0        |      |            |          |
      | 15     |             | 31.0 | 12.0       | 15.0     |
      | 18     |             | 31.0 | 15.0       | 18.0     |
  #    When the application is restarted at 22
  #    Then these events occurs before system tick:
  #      | minute | temperature | heat | heat.Start | heat.End |
  #      | 23     | 10          |      |            |          |
  #      | 24     |             | 10.0 | 18.0       | 24.0     |

  Scenario: Gauge should not publish events with the right start and end after restart
    When the service is started
    Then these events occurs before system tick:
      | minute | temperature | heat | heat.Start | heat.End |
      | 1      | 10.0        |      |            |          |
      | 2      | 15.0        |      |            |          |
      | 3      | 13.0        | 13.0 | 0.0        | 3.0      |
      | 4      | 20.0        |      |            |          |
    When the application is restarted at 5
    Then I get 0 TimeMaths Event
    Then these events occurs before system tick:
      | minute | temperature | heat | heat.Start | heat.End |
      | 6      | 35.0        | 35.0 | 3.0        | 6.0      |
      | 9      | 35.0        | 35.0 | 6.0        | 9.0      |

  @Ignore
  Scenario: Gauge should publish events with the right start and a end to last receive value after restart
    When the service is started
    Then these events occurs before system tick:
      | minute | temperature | heat | heat.Start | heat.End |
      | 1      | 10.0        |      |            |          |
      | 2      | 15.0        |      |            |          |
      | 3      | 13.0        | 13.0 | 0.0        | 3.0      |
      | 4      | 20.0        |      |            |          |
    When the application is restarted at 5
    Then I get 0 TimeMaths Event
    Then these events occurs before system tick:
      | minute | temperature | heat | heat.Start | heat.End |
      | 6      |             | 20.0 | 3.0        | 4.0      |
      | 9      | 35.0        | 35.0 | 6.0        | 9.0      |

  Scenario: Gauge should publish events with the right start and end after restart
    When the service is started
    Then these events occurs before system tick:
      | minute | temperature | heat | heat.Start | heat.End |
      | 1      | 10.0        |      |            |          |
      | 2      | 15.0        |      |            |          |
      | 3      | 13.0        | 13.0 | 0.0        | 3.0      |
      | 4      | 20.0        |      |            |          |
      | 5      | 30.0        |      |            |          |
    When the application is restarted at 7
    Then these events occurs before system tick:
      | minute | temperature | heat | heat.Start | heat.End |
      | 7      |             | 30.0 | 3.0        | 5.0      |
      | 8      | 32.0        |      |            |          |
      | 9      |             | 32.0 | 7.0        | 9.0      |
      | 10     | 38.0        |      |            |          |
      | 11     | 40.0        |      |            |          |
      | 12     |             | 40.0 | 9.0        | 12.0     |
      | 14     | 31.0        |      |            |          |
      | 15     |             | 31.0 | 12.0       | 15.0     |

  @Ignore
  Scenario: Gauge should not publish events with the right start and wait next publish
  #TODO: this case will should be discussed
    When the service is started
    Then these events occurs before system tick:
      | minute | temperature | heat | heat.Start | heat.End |
      | 1      | 10.0        |      |            |          |
      | 2      | 15.0        |      |            |          |
      | 3      | 13.0        | 13.0 | 0.0        | 3.0      |
    When the application is restarted at 7
    Then I get 0 TimeMaths Event
    When the time now 9
  #| 9      |             | 13.0 | 3.0        | 9.0      |?
  #| 9      |             | 13.0 | 3.0        | 3.0      |?
  #| 9      |             | 13.0 | 7.0        | 9.0      |?
    Then these events occurs before system tick:
      | minute | temperature | heat | heat.Start | heat.End |
      | 10     | 38.0        |      |            |          |
      | 11     | 40.0        |      |            |          |
      | 12     |             | 40.0 | 9.0        | 12.0     |
      | 14     | 31.0        |      |            |          |
      | 15     |             | 31.0 | 12.0       | 15.0     |
