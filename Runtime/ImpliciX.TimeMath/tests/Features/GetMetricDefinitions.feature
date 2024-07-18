@Ignore

Feature: GetMetricDefinitions
As a designer
I want to have a view of all the urn for a metrics

  Scenario: All the URN for a Gauge
  The gauge is a simple sampling of any numeric value.
  At the end of each period is published the latest known value of the monitored urn value
    Given the "analytics:instrumentation:electrical_index" service primary period is define to 3 minutes
    And a "instrumentation:electrical_index:measure" Gauge Computer
    When it ask for URNs
    Then The system returns:
      | analytics:instrumentation:electrical_index |

  Scenario: All the URN for a Variation
  The variation computes the increases and decreases of any numeric value.
  At the end of each period is published the variation of the monitored urn value across the period.
    Given the "analytics:production:main_circuit:return_temperature" service primary period is define to 3 minutes
    And a "instrumentation:electrical_index:measure" Variation Computer
    When it ask for URNs
    Then The system returns:
      | analytics:instrumentation:electrical_index_delta |

  Scenario: All the URN for a Accumulator
  The accumulator gathers data about a single numeric urn value.
  At the end of each period are published:

  accumulated_value: the sum of all received values for the period
  samples_count: the number of received values for the period
  These data allow to compute offline averages across arbitrary length of time which only need to be multiples of the metric period.
    Given the "analytics:production:main_circuit:return_temperature" service primary period is define to 3 minutes
    And a "production:main_circuit:return_temperature:measure" Accumulator Computer
    When it ask for URNs
    Then The system returns:
      | analytics:production:main_circuit:return_temperature:accumulated_value |
      | analytics:production:main_circuit:return_temperature:samples_count     |

  Scenario: All the URN for a simple State monitoring
  The state monitoring monitors a single state value that is an enum/integer value (e.g. the current state of a state machine)
  At the end of each period are published:

  occurrence: the number of times the considered state was entered in the period
  duration: the total time spent in the given state during the period
  If a state was not entered during the period, the corresponding values are null and are not published.
    Given the "analytics:service:heating:public_state" is an enum with values:
      | Disabled |
      | Active   |
    Given the "analytics:service:heating:public_state" service primary period is define to 3 minutes
    And a "service:heating:public_state" State monitoring Computer
    When it ask for URNs
    Then The system returns:
      | analytics:service:heating:public_state:Disabled:occurrence |
      | analytics:service:heating:public_state:Disabled:duration   |
      | analytics:service:heating:public_state:Active:occurrence   |
      | analytics:service:heating:public_state:Active:duration     |

  Scenario: All the URN for a simple State monitoring including one accumulator computer
  Additional computations such as gauge, variations and accumulators can be included in state monitoring.
  At the end of each period these computations are published for each state observed during the period.

  If a state was not entered during the period, the corresponding values are null and are not published.
    Given the "analytics:service:heating:public_state" is an enum with values:
      | Disabled |
      | Active   |
    Given the "analytics:service:heating:public_state" service primary period is define to 3 minutes
    And a "service:heating:public_state" State monitoring Computer
    And including "supply_temperature" as Accumulator of "production:main_circuit:supply_temperature:measure"
    When it ask for infos
    Then The system returns:
      | analytics:service:heating:public_state:Disabled:occurrence                           |
      | analytics:service:heating:public_state:Disabled:duration                             |
      | analytics:service:heating:public_state:Disabled:supply_temperature:accumulated_value |
      | analytics:service:heating:public_state:Disabled:supply_temperature:samples_count     |
      | analytics:service:heating:public_state:Active:occurrence                             |
      | analytics:service:heating:public_state:Active:duration                               |
      | analytics:service:heating:public_state:Active:supply_temperature:accumulated_value   |
      | analytics:service:heating:public_state:Active:supply_temperature:samples_count       |

  Scenario: All the URN for a simple State monitoring including other computer
  Additional computations such as gauge, variations and accumulators can be included in state monitoring.
  At the end of each period these computations are published for each state observed during the period.

  If a state was not entered during the period, the corresponding values are null and are not published.
    Given the "analytics:service:heating:public_state" is an enum with values:
      | Disabled |
      | Active   |
    Given the "analytics:service:heating:public_state" service primary period is define to 3 minutes
    And a "service:heating:public_state" State monitoring Computer
    And including "gas_index_delta" as Variation of "instrumentation:gas_index:measure"
    And including "electrical_index_delta" as Variation of "instrumentation:electrical_index:measure"
    And including "supply_temperature" as Accumulator of "production:main_circuit:supply_temperature:measure"
    And including "return_temperature" as Accumulator of "production:main_circuit:return_temperature:measure"
    When it ask for URNs
    Then The system returns:
      | analytics:service:heating:public_state:Disabled:occurrence                           |
      | analytics:service:heating:public_state:Disabled:duration                             |
      | analytics:service:heating:public_state:Disabled:gas_index_delta                      |
      | analytics:service:heating:public_state:Disabled:electrical_index_delta               |
      | analytics:service:heating:public_state:Disabled:supply_temperature:accumulated_value |
      | analytics:service:heating:public_state:Disabled:supply_temperature:samples_count     |
      | analytics:service:heating:public_state:Disabled:return_temperature:accumulated_value |
      | analytics:service:heating:public_state:Disabled:return_temperature:samples_count     |
      | analytics:service:heating:public_state:Active:occurrence                             |
      | analytics:service:heating:public_state:Active:duration                               |
      | analytics:service:heating:public_state:Active:gas_index_delta                        |
      | analytics:service:heating:public_state:Active:electrical_index_delta                 |
      | analytics:service:heating:public_state:Active:supply_temperature:accumulated_value   |
      | analytics:service:heating:public_state:Active:supply_temperature:samples_count       |
      | analytics:service:heating:public_state:Active:return_temperature:accumulated_value   |
      | analytics:service:heating:public_state:Active:return_temperature:samples_count       |

  Scenario: All the URN for a Variation Storage
  All computed metric values can be stored over an arbitrary length of time
    Given the "analytics:production:main_circuit:return_temperature" service primary period is define to 3 minutes
    And a "instrumentation:electrical_index:measure" Variation Computer
    And it is over the past 5 days
    When it ask for URNs
    Then The system returns:
      | analytics:instrumentation:electrical_index_delta |

  Scenario: All the URN for a Variation Grouping
  All metric values can be aggregated into larger periods of time
    Given the "analytics:production:main_circuit:return_temperature" service primary period is define to 3 minutes
    And a "instrumentation:electrical_index:measure" Variation Computer
    And a group daily
    And a group every 2 weeks
    When it ask for URNs
    Then The system returns:
      | analytics:instrumentation:electrical_index_delta         |
      | analytics:instrumentation:electrical_index_delta:_1day   |
      | analytics:instrumentation:electrical_index_delta:_2weeks |

  Scenario: All the URN for a Variation Group and store
  All groupings can also be stored
    Given the "analytics:production:main_circuit:return_temperature" service primary period is define to 3 minutes
    And a "instrumentation:electrical_index:measure" Variation Computer
    And a group daily over the past 4 weeks
    And a group every 2 weeks over the past 5 days
    When it ask for URNs
    Then The system returns:
      | analytics:instrumentation:electrical_index_delta         |
      | analytics:instrumentation:electrical_index_delta:_1day   |
      | analytics:instrumentation:electrical_index_delta:_2weeks |
