Feature: Multiple restarts

    Background:
        Given a TimeMath service start at 20 minutes
        And the "delta" service primary period is define to 3 minutes
        Given a "temperature" Variation Computer

    Scenario: 1 Off during publish, no publish, 1 Off between two publish
        When the service is started
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 20     | 10          |       |             |           |
          | 21     | 15          | 5     | 20          | 21        |
          | 23     | 22          |       |             |           |
        When the time now 23.5
        When the application is restarted at 25
        Then I get 1 TimeMaths Event
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 25     |             | 7     | 21          | 23.5      |
        When the time now 25.75
        When the application is restarted at 26.5
        Then I get 0 TimeMaths Event
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 27     | 18          | -4    | 25          | 27        |
          | 28     | 7           |       |             |           |
          | 30     |             | -11   | 27          | 30        |
          | 32     | 14          |       |             |           |
          | 33     |             | 7     | 30          | 33        |
          | 35     | 25          |       |             |           |
          | 36     |             | 11    | 33          | 36        |

    Scenario: 1 Off during publish, publish value, 1 Off between two publish, then last value between the 2 off is not ignored
        When the service is started
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 20     | 10          |       |             |           |
          | 21     | 15          | 5     | 20          | 21        |
          | 23     | 22          |       |             |           |
        When the time now 23.5
        When the application is restarted at 25
        Then I get 1 TimeMaths Event
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 25     |             | 7     | 21          | 23.5      |
          | 26     | 18          |       |             |           |
          | 27     |             | -4    | 25          | 27        |
        When the time now 27.25
        When the application is restarted at 28.75
        Then I get 0 TimeMaths Event
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 29     | 7           |       |             |           |
          | 30     |             | -11   | 27          | 30        |
          | 32     | 14          |       |             |           |
          | 33     |             | 7     | 30          | 33        |
          | 35     | 25          |       |             |           |
          | 36     |             | 11    | 33          | 36        |