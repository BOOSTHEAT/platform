Feature: Restart when Variation Windowed is off AFTER the first publish

    Background:
        Given a TimeMath service start at 1 minutes
        And the "delta" service primary period is define to 3 minutes
        And the service has a window period of 9 minutes
        Given a "temperature" Variation Computer

    Scenario: Off between 1st and 2nd publish, and no value received between restart and 2nd publish,
    then i use shutdown time as sampling end on 2nd publish
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
          | 6      |             | 10    | 1           | 4         |
          | 8      | 32          |       |             |           |
          | 9      |             | 22    | 1           | 9         |
          | 11     | 14          |       |             |           |
          | 12     |             | 1     | 3           | 12        |
          | 13     | 2           |       |             |           |
          | 15     |             | -18   | 6           | 15        |
          | 17     | 12          |       |             |           |
          | 18     |             | -20   | 9           | 18        |

    Scenario: Off between 1st and 2nd publish, and new value received between restart and 2nd publish,
    then i publish like no restart was append
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
          | 6      | 17          | 7     | 1           | 6         |
          | 8      | 32          |       |             |           |
          | 9      |             | 22    | 1           | 9         |
          | 11     | 14          |       |             |           |
          | 12     |             | 1     | 3           | 12        |
          | 13     | 2           |       |             |           |
          | 15     |             | -15   | 6           | 15        |
          | 17     | 12          |       |             |           |
          | 18     |             | -20   | 9           | 18        |

    Scenario: Off during 2nd publish, then i publish at restart time
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
          | 7      |             | 10    | 1           | 4         |
          | 8      | 32          |       |             |           |
          | 9      |             | 22    | 1           | 9         |
          | 11     | 14          |       |             |           |
          | 12     |             | 1     | 3           | 12        |
          | 13     | 2           |       |             |           |
          | 15     |             | -18   | 7           | 15        |
          | 17     | 12          |       |             |           |
          | 18     |             | -20   | 9           | 18        |

    Scenario: Off during 2nd and 3rd publish, then i publish at restart time
        When the service is started
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 1      | 10          |       |             |           |
          | 2      | 15          |       |             |           |
          | 3      | 13          | 3     | 1           | 3         |
          | 4      | 20          |       |             |           |
        When the application is restarted at 10
        Then I get 1 TimeMaths Event
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 10     |             | 10    | 1           | 4         |
          | 11     | 32          |       |             |           |
          | 12     |             | 19    | 3           | 12        |
          | 13     | 14          |       |             |           |
          | 15     |             | -6    | 10          | 15        |
          | 13     | 2           |       |             |           |
          | 18     |             | -18   | 10          | 18        |
          | 17     | 12          |       |             |           |
          | 21     |             | -20   | 12          | 21        |
          | 24     | 7           | -7    | 15          | 24        |

    Scenario: Off during all window time and restart on window end that was expected, then i publish at restart time
        When the service is started
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 1      | 10          |       |             |           |
          | 2      | 15          |       |             |           |
          | 3      | 13          | 3     | 1           | 3         |
          | 4      | 20          |       |             |           |
        When the application is restarted at 12
        Then I get 1 TimeMaths Event
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 12     |             | 10    | 1           | 4         |
          | 13     | 14          |       |             |           |
          | 15     |             | -6    | 12          | 15        |
          | 13     | 2           |       |             |           |
          | 18     |             | -18   | 12          | 18        |
          | 17     | 12          |       |             |           |
          | 21     |             | -8    | 12          | 21        |
          | 24     | 7           | -7    | 15          | 24        |
          | 27     | 4           | 2     | 18          | 27        |

    Scenario: Off during 2nd, 3rd and 4th publish, then i publish at restart time
        When the service is started
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 1      | 10          |       |             |           |
          | 2      | 15          |       |             |           |
          | 3      | 13          | 3     | 1           | 3         |
          | 4      | 20          |       |             |           |
        When the application is restarted at 13
        Then I get 1 TimeMaths Event
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 13     |             | 10    | 1           | 4         |
          | 15     | 17          | -3    | 13          | 15        |
          | 13     | 2           |       |             |           |
          | 18     |             | -18   | 13          | 18        |
          | 17     | 12          |       |             |           |
          | 21     |             | -8    | 13          | 21        |
          | 24     | 5           | -12   | 15          | 24        |
          | 27     | 7           | 5     | 18          | 27        |

    Scenario: Off during more than 1 window retention period, then i publish at restart time
        When the service is started
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 1      | 10          |       |             |           |
          | 2      | 15          |       |             |           |
          | 3      | 13          | 3     | 1           | 3         |
          | 4      | 20          |       |             |           |
        When the application is restarted at 20
        Then I get 1 TimeMaths Event
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 20     |             | 10    | 1           | 4         |
          | 21     | 17          | -3    | 20          | 21        |
          | 23     | 2           |       |             |           |
          | 24     |             | -18   | 20          | 24        |
          | 26     | 12          |       |             |           |
          | 27     |             | -8    | 20          | 27        |
          | 30     | 5           | -12   | 21          | 30        |
          | 33     | 19          | 17    | 24          | 33        |

    Scenario: Off when window just slided
        When the service is started
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 1      | 10          |       |             |           |
          | 2      | 15          |       |             |           |
          | 3      | 13          | 3     | 1           | 3         |
          | 4      | 20          |       |             |           |
          | 6      |             | 10    | 1           | 6         |
          | 8      | 32          |       |             |           |
          | 9      |             | 22    | 1           | 9         |
        When the application is restarted at 10
        Then I get 0 TimeMaths Event
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 12     |             | 19    | 3           | 12        |
          | 14     | 2           |       |             |           |
          | 15     |             | -18   | 6           | 15        |
          | 17     | 12          |       |             |           |
          | 18     |             | -20   | 9           | 18        |
          | 20     | 7           |       |             |           |
          | 21     |             | -25   | 12          | 21        |
          | 22     | 8           |       |             |           |
          | 24     |             | 6     | 15          | 24        |
          | 27     | 11          | -1    | 18          | 27        |