Feature: Restart when Variation stop AFTER the first publish

    Background:
        Given a TimeMath service start at 1 minutes
        And the "delta" service primary period is define to 3 minutes
        Given a "temperature" Variation Computer

    Scenario: Off between 1st and 2nd publish, and no value received between restart and publish,
    then i wait the next publish time to publish
        When the service is started
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 1      | 10          |       |             |           |
          | 2      | 15          |       |             |           |
          | 3      | 13          | 3     | 1           | 3         |
          | 4      | 20          |       |             |           |
        When the application is restarted at 5
        Then I get 0 TimeMaths Event
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 6      |             | 7     | 3           | 4         |
          | 9      |             | 0     | 6           | 9         |

    Scenario: Off between 1st and 2nd publish, and new value received between restart and publish,
    then i wait the next publish time to publish
        When the service is started
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 1      | 10          |       |             |           |
          | 2      | 15          |       |             |           |
          | 3      | 13          | 3     | 1           | 3         |
          | 4      | 20          |       |             |           |
        When the application is restarted at 5
        Then I get 0 TimeMaths Event
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 6      | 14          | 1     | 3           | 6         |
          | 8      | 32          |       |             |           |
          | 9      |             | 18    | 6           | 9         |

    Scenario: Off during 2nd publish, and no value received between restart and publish,
    then i publish at restart time
        When the service is started
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 1      | 10          |       |             |           |
          | 2      | 15          |       |             |           |
          | 3      | 13          | 3     | 1           | 3         |
          | 4      | 20          |       |             |           |
        When the application is restarted at 7
        Then I get 1 TimeMaths Event
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 7      |             | 7     | 3           | 4         |
          | 9      |             | 0     | 7           | 9         |

    Scenario: Off during 2nd publish, and new value received between restart and publish,
    then i publish at restart time
        When the service is started
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 1      | 10          |       |             |           |
          | 2      | 15          |       |             |           |
          | 3      | 13          | 3     | 1           | 3         |
          | 4      | 20          |       |             |           |
        When the application is restarted at 7
        Then I get 1 TimeMaths Event
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 7      |             | 7     | 3           | 4         |
          | 9      | 32          | 12    | 7           | 9         |