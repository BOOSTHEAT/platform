Feature: Windowed Multiple restarts

  Background:
    Given a TimeMath service start at 20 minutes
    And the "delta" service primary period is define to 3 minutes
    And the service has a window period of 15 minutes
    Given a "temperature" Variation Computer
  
    Scenario: 1 Off during publish, no publish, 1 Off between two publish
        When the service is started
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 20     | 10          |       |             |           |
          | 21     | 15          | 5     | 20          | 21        |
          | 23     | 23          |       |             |           |
        When the time now 23.5
        When the application is restarted at 25
        Then I get 1 TimeMaths Event
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 25     |             | 13    | 20          | 23.5      |
        When the time now 25.75
        When the application is restarted at 26.5
        Then I get 0 TimeMaths Event
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 27     | 18          | 8     | 20          | 27        |
          | 28     | 7           |       |             |           |
          | 30     |             | -3    | 20          | 30        |
          | 32     | 14          |       |             |           |
          | 33     |             | 4     | 20          | 33        |
          | 35     | 25          |       |             |           |
          | 36     |             | 10    | 21          | 36        |
          | 38     | 11          |       |             |           |
          | 39     |             | -12   | 25          | 39        |
          | 42     | 1           | -17   | 27          | 42        |
          | 44     | 19          |       |             |           |
          | 45     |             | 12    | 30          | 45        |

    Scenario: 1 Off during publish, publish value, 1 Off between two publish, then last value between the 2 off is not ignored
        When the service is started
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 20     | 10          |       |             |           |
          | 21     | 15          | 5     | 20          | 21        |
          | 23     | 23          |       |             |           |
        When the time now 23.5
        When the application is restarted at 25
        Then I get 1 TimeMaths Event
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 25     |             | 13    | 20          | 23.5      |
          | 26     | 18          |       |             |           |
          | 27     |             | 8     | 20          | 27        |
        When the time now 27.25
        When the application is restarted at 28.75
        Then I get 0 TimeMaths Event
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 29     | 7           |       |             |           |
          | 30     |             | -3    | 20          | 30        |
          | 32     | 14          |       |             |           |
          | 33     |             | 4     | 20          | 33        |
          | 35     | 25          |       |             |           |
          | 36     |             | 10    | 21          | 36        |
          | 38     | 11          |       |             |           |
          | 39     |             | -12   | 25          | 39        |
          | 42     | 1           | -17   | 27          | 42        |
          | 44     | 19          |       |             |           |
          | 45     |             | 12    | 30          | 45        |