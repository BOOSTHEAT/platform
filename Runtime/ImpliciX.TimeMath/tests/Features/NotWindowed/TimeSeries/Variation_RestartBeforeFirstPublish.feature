Feature: Restart when Variation stop BEFORE the first publish

    Background:
        Given a TimeMath service start at 1 minutes
        And the "delta" service primary period is define to 3 minutes
        Given a "temperature" Variation Computer

    Scenario: Off BEFORE the first publish, and no value received between restart and publish,
    then i wait the next publish time to publish and i use sampling end when app shutdown in event
        When the service is started
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 1      | 10          |       |             |           |
          | 1.5    | 15          |       |             |           |
        When the time now 1.75
        When the application is restarted at 2
        Then I get 0 TimeMaths Event
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 3      |             | 5     | 1           | 1.75      |
          | 5      | 23          |       |             |           |
          | 6      |             | 8     | 3           | 6         |

    Scenario: Off BEFORE the first publish, and new value received between restart and publish,
    then i wait the next publish time to publish and i use publish time as sampling end in event
        When the service is started
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 1      | 10          |       |             |           |
          | 1.5    | 15          |       |             |           |
        When the application is restarted at 2
        Then I get 0 TimeMaths Event
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 3      | 17          | 7     | 1           | 3         |
          | 5      | 23          |       |             |           |
          | 6      |             | 6     | 3           | 6         |

    Scenario: Off during the first publish time expected, and no value are received between restart and publish,
    then i wait the next publish time to publish and i use sampling end when app shutdown in event
        When the service is started
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 1      | 10          |       |             |           |
          | 1.5    | 15          |       |             |           |
        When the time now 1.75
        When the application is restarted at 4
        Then I get 1 TimeMaths Event
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 4      | 8           | 5     | 1           | 1.75      |
          | 6      |             | -7    | 4           | 6         |
          | 8      | 21          |       |             |           |
          | 9      |             | 13    | 6           | 9         |

    Scenario: Off during the first publish time expected, and new value received between restart and publish,
    then i wait the next publish time to publish and i use publish time as sampling end in event
        When the service is started
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 1      | 10          |       |             |           |
          | 1.5    | 15          |       |             |           |
        When the application is restarted at 4
        Then I get 1 TimeMaths Event
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 4      | 17          | 5     | 1           | 1.5       |
          | 6      |             | 2     | 4           | 6         |
          | 7      | 21          |       |             |           |
          | 9      |             | 4     | 6           | 9         |