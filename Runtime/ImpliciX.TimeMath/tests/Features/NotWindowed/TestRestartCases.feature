Feature: TestRestartCases
TimeMath computers should return special start and end timing du to restart

  Scenario Outline: during nominal run the start of the period  is always equals to the last publish
    Given a TimeMath service start at 97 minutes
    And the "output" service primary period is define to 3 minutes
    Given a "input" <type> Computer
    When the service is started
    Then these events occurs before system tick:
      | minute | input | output.Start | output.End |
      | 97     | 320   |              |            |
      | 98     | 67    |              |            |
      | 99     | 115   | 97           | 99         |
      | 100    | 246   |              |            |
      | 101    | 8     |              |            |
      | 102    | 328   | 99           | 102        |
      | 103    | 33    |              |            |
      | 104    | 290   |              |            |
      | 105    | 145   | 102          | 105        |
      | 106    | 103   |              |            |
      | 107    | 173   |              |            |
      | 108    | 139   | 105          | 108        |
      | 109    | 83    |              |            |
      | 110    | 358   |              |            |
      | 111    | 140   | 108          | 111        |
      | 112    | 131   |              |            |
      | 113    | 396   |              |            |
      | 114    | 484   | 111          | 114        |
      | 115    | 357   |              |            |
      | 116    | 195   |              |            |
      | 117    | 16    | 114          | 117        |
      | 118    | 384   |              |            |
      | 119    | 404   |              |            |
      | 120    | 384   | 117          | 120        |
      | 121    | 425   |              |            |
      | 122    | -22   |              |            |
      | 123    | 287   | 120          | 123        |

    Examples:
      | type        |
      | Gauge       |
      | Accumulator |
  #      | Variation   |

  Scenario Outline: After restart during publishing period the end of the period after restart is the last update value time
    Given a TimeMath service start at 97 minutes
    And the "output" service primary period is define to 3 minutes
    Given a "input" <type> Computer
    When the service is started
    Then these events occurs before system tick:
      | minute | input | output.Start | output.End |
      | 97     | 237   |              |            |
      | 98     | 388   |              |            |
      | 99     | 19    | 97           | 99         |
      | 100    | 352   |              |            |
    When the application is restarted at 101
    Then I get 0 TimeMaths Event
    Then these events occurs before system tick:
      | minute | input | output.Start | output.End |
      | 102    | 246   | 99           | 102        |
      | 103    | 14    |              |            |
      | 104    | 209   |              |            |
      | 105    | 233   | 102          | 105        |
      | 106    | 287   |              |            |
      | 107    | 40    |              |            |
      | 108    | 271   | 105          | 108        |
      | 109    | 95    |              |            |
      | 110    | 304   |              |            |
      | 111    | 279   | 108          | 111        |
      | 112    | 77    |              |            |
      | 113    | 156   |              |            |
      | 114    | 171   | 111          | 114        |
      | 115    | 53    |              |            |
      | 116    | 134   |              |            |
      | 117    | 189   | 114          | 117        |
      | 118    | 198   |              |            |
      | 119    | 265   |              |            |
      | 120    | 328   | 117          | 120        |
      | 121    | -28   |              |            |
      | 122    | 154   |              |            |
      | 123    | 138   | 120          | 123        |

    Examples:
      | type        |
      | Gauge       |
      | Accumulator |
  #      | Variation   |

  Scenario Outline: After restart during publishing period the end of the period after restart is the restart time
    Given a TimeMath service start at 97 minutes
    And the "output" service primary period is define to 3 minutes
    Given a "input" <type> Computer
    When the service is started
    Then these events occurs before system tick:
      | minute | input | output.Start | output.End |
      | 97     | 237   |              |            |
      | 98     | 388   |              |            |
      | 99     | 19    | 97           | 99         |
      | 100    | 352   |              |            |
    When the application is restarted at 101
    Then I get 0 TimeMaths Event
    Then these events occurs before system tick:
      | minute | input | output.Start | output.End |
      | 102    |       | 99           | 100        |
      | 103    | 14    |              |            |
      | 104    | 209   |              |            |
      | 105    | 233   | 102          | 105        |
      | 106    | 287   |              |            |
      | 107    | 40    |              |            |
      | 108    | 271   | 105          | 108        |
      | 109    | 95    |              |            |
      | 110    | 304   |              |            |
      | 111    | 279   | 108          | 111        |
      | 112    | 77    |              |            |
      | 113    | 156   |              |            |
      | 114    | 171   | 111          | 114        |
      | 115    | 53    |              |            |
      | 116    | 134   |              |            |
      | 117    | 189   | 114          | 117        |
      | 118    | 198   |              |            |
      | 119    | 265   |              |            |
      | 120    | 328   | 117          | 120        |
      | 121    | -28   |              |            |
      | 122    | 154   |              |            |
      | 123    | 138   | 120          | 123        |

    Examples:
      | type        |
      | Gauge       |
      | Accumulator |
  #      | Variation   |

  Scenario Outline: After restart after the publishing period a the values are publish on restart
    Given a TimeMath service start at 97 minutes
    And the "output" service primary period is define to 3 minutes
    Given a "input" <type> Computer
    When the service is started
    Then these events occurs before system tick:
      | minute | input | output.Start | output.End |
      | 97     | 255   |              |            |
      | 98     | -19   |              |            |
      | 99     | 108   | 97           | 99         |
      | 100    | 292   |              |            |
    When the application is restarted at 103
    Then these events occurs before system tick:
      | minute | input | output.Start | output.End |
      | 103    |       | 99           | 100        |
      | 104    | 393   |              |            |
      | 105    | 66    | 103          | 105        |
      | 106    | 269   |              |            |
      | 107    | 209   |              |            |
      | 108    | 221   | 105          | 108        |
      | 109    | 285   |              |            |
      | 110    | 305   |              |            |
      | 111    | 80    | 108          | 111        |
      | 112    | 124   |              |            |
      | 113    | 39    |              |            |
      | 114    | 442   | 111          | 114        |
      | 115    | 460   |              |            |
      | 116    | 191   |              |            |
      | 117    | 239   | 114          | 117        |
      | 118    | -10   |              |            |
      | 119    | 329   |              |            |
      | 120    | 406   | 117          | 120        |
      | 121    | 393   |              |            |
      | 122    | 66    |              |            |
      | 123    | 269   | 120          | 123        |

    Examples:
      | type        |
      | Gauge       |
      | Accumulator |
  #      | Variation   |
