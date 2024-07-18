Feature: Restart when Variation Windowed is off BEFORE the first publish

    Background:
        Given a TimeMath service start at 1 minutes
        And the "delta" service primary period is define to 3 minutes
        And the service has a window period of 9 minutes
        Given a "temperature" Variation Computer

    Scenario: No value received between restart and 1st publish, then i use shutdown time as sampling end on 1st publish
        When the service is started
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 1      | 10          |       |             |           |
          | 1.5    | 15          |       |             |           |
        When the application is restarted at 2
        Then I get 0 TimeMaths Event
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 3      |             | 5     | 1           | 1.5       |
          | 5      | 23          |       |             |           |
          | 6      |             | 13    | 1           | 6         |
          | 8      | 18          |       |             |           |
          | 9      |             | 8     | 1           | 9         |
          | 11     | 14          |       |             |           |
          | 12     |             | -1    | 3           | 12        |
          | 15     | 27          | 4     | 6           | 15        |

    Scenario: New value received between restart and 1st publish, then i wait the publish time to publish
        When the service is started
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 1      | 10          |       |             |           |
          | 1.5    | 15          |       |             |           |
        When the application is restarted at 2
        Then I get 0 TimeMaths Event
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 2.5    | 7           |       |             |           |
          | 3      |             | -3    | 1           | 3         |
          | 5      | 23          |       |             |           |
          | 6      |             | 13    | 1           | 6         |
          | 8      | 18          |       |             |           |
          | 9      |             | 8     | 1           | 9         |
          | 11     | 14          |       |             |           |
          | 12     |             | 7     | 3           | 12        |
          | 15     | 27          | 4     | 6           | 15        |

    Scenario: Off during the first publish expected, then i publish at restart time
        When the service is started
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 1      | 10          |       |             |           |
          | 1.5    | 7           |       |             |           |
        When the application is restarted at 4
        Then I get 1 TimeMaths Event
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 4      |             | -3    | 1           | 1.5       |
          | 5      | 21          |       |             |           |
          | 6      |             | 11    | 1           | 6         |
          | 8      | 18          |       |             |           |
          | 9      |             | 8     | 1           | 9         |
          | 11     | 14          |       |             |           |
          | 12     |             | 7     | 4           | 12        |
          | 13     | 9           |       |             |           |
          | 15     |             | -12   | 6           | 15        |
          | 18     | 1           | -17   | 9           | 18        |