@Configuration

Feature: Configuration
As Application designer
I want to define a TimeMath service
So that I will be able to view values

  Background:
    Given a TimeMath service
    And the "heat" service primary period is define to 3 minutes

  @Ignore
  Scenario: The Window period should not be shorter than the primary period in variation
    Given the service has a window period of 2 minutes
    When the service is initialize
    Then I get the error "Window period of Metric must be greater than primary publication period"

  @Ignore
  Scenario: The Window period should not be equal to the primary period in variation
    Given the service has a window period of 3 minutes
    When the service is initialize
    Then I get the error "Window period of Metric must be greater than primary publication period"

  @Ignore
  Scenario: The Window period should greater than the primary period in variation
    Given the service has a window period of 6 minutes
    When the service is initialize
    Then I get TimeMaths event

  @Ignore
  Scenario: A TimeMath without a Window period in variation should start
    Given a "temperature" Variation Computer
    When the service is initialize
    Then I get TimeMaths event

  Scenario: A TimeMath without a Window period in gauge should start
    Given a "temperature" Gauge Computer
    When the service is initialize
    Then I get TimeMaths event

  Scenario: A TimeMath without a Window period in accumulation should start
    Given a "temperature" Accumulator Computer
    When the service is initialize
    Then I get TimeMaths event

  Scenario: The Window period should not be shorter than the primary period in accumulator
    Given the service has a window period of 2 minutes
    And a "temperature" Accumulator Computer
    When the service is initialize
    Then I get the error "Window period of Metric must be greater than primary publication period"

  Scenario: The Window period should not be equal to the primary period in accumulator
    Given the service has a window period of 3 minutes
    And a "temperature" Accumulator Computer
    When the service is initialize
    Then I get the error "Window period of Metric must be greater than primary publication period"

  Scenario: The Window period should greater than the primary period in accumulator
    Given the service has a window period of 6 minutes
    And a "temperature" Accumulator Computer
    When the service is initialize
    Then I get TimeMaths event

  Scenario: The Window period should be a multiplier of the primary period in accumulator
    Given the service has a window period of 5 minutes
    And a "temperature" Accumulator Computer
    When the service is initialize
    Then I get the error "Window period of Metric must be a multiplier of the primary publication period"

  Scenario: A TimeMath without a Window period in accumulator should start
    Given a "temperature" Accumulator Computer
    When the service is initialize
    Then I get TimeMaths event
