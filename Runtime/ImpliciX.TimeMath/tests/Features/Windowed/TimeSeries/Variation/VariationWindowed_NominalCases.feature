Feature: Windowed variation nominal cases

    Scenario: Start at 0 minute
        Given a TimeMath service start at 0 minutes
        And the "delta" service primary period is define to 3 minutes
        And the service has a window period of 9 minutes
        Given a "temperature" Variation Computer
        When the service is started
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 1      | 10          |       |             |           |
          | 1.5    | 15          |       |             |           |
          | 3      |             | 5     | 0           | 3         |
          | 5      | 23          |       |             |           |
          | 6      |             | 13    | 0           | 6         |
          | 8      | 18          |       |             |           |
          | 9      |             | 8     | 0           | 9         |
          | 11     | 14          |       |             |           |
          | 12     |             | -1    | 3           | 12        |
          | 15     |             | -9    | 6           | 15        |

    Scenario: Start before 1 period time with window = 3 periods 
        Given a TimeMath service start at 1 minutes
        And the "delta" service primary period is define to 3 minutes
        And the service has a window period of 9 minutes
        Given a "temperature" Variation Computer
        When the service is started
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 1      | 10          |       |             |           |
          | 1.5    | 15          |       |             |           |
          | 3      |             | 5     | 1           | 3         |
          | 5      | 23          |       |             |           |
          | 6      |             | 13    | 1           | 6         |
          | 8      | 18          |       |             |           |
          | 9      |             | 8     | 1           | 9         |
          | 11     | 14          |       |             |           |
          | 12     |             | -1    | 3           | 12        |
          | 13     | 25          |       |             |           |
          | 15     |             | 2     | 6           | 15        |
          | 18     | 11          | -7    | 9           | 18        |
          | 21     | 1           | -13   | 12          | 21        |
          | 24     | 19          | -6    | 15          | 24        |

    Scenario: Start after 1 period time with window = 3 periods 
        Given a TimeMath service start at 4 minutes
        And the "delta" service primary period is define to 3 minutes
        And the service has a window period of 9 minutes
        Given a "temperature" Variation Computer
        When the service is started
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 4      | 10          |       |             |           |
          | 5      | 23          |       |             |           |
          | 6      |             | 13    | 4           | 6         |
          | 8      | 18          |       |             |           |
          | 9      |             | 8     | 4           | 9         |
          | 11     | 14          |       |             |           |
          | 12     |             | 4     | 4           | 12        |
          | 13     | 25          |       |             |           |
          | 15     |             | 2     | 6           | 15        |
          | 18     | 11          | -7    | 9           | 18        |
          | 21     | 1           | -13   | 12          | 21        |
          | 24     | 19          | -6    | 15          | 24        |

    Scenario: Start after 2 periods time with window = 3 periods 
        Given a TimeMath service start at 8 minutes
        And the "delta" service primary period is define to 3 minutes
        And the service has a window period of 9 minutes
        Given a "temperature" Variation Computer
        When the service is started
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 8      | 10          |       |             |           |
          | 8.5    | 18          |       |             |           |
          | 9      |             | 8     | 8           | 9         |
          | 11     | 14          |       |             |           |
          | 12     |             | 4     | 8           | 12        |
          | 13     | 25          |       |             |           |
          | 15     |             | 15    | 8           | 15        |
          | 18     | 11          | -7    | 9           | 18        |
          | 21     | 1           | -13   | 12          | 21        |
          | 24     | 19          | -6    | 15          | 24        |

    Scenario: Start on window retention time with window = 3 periods 
        Given a TimeMath service start at 9 minutes
        And the "delta" service primary period is define to 3 minutes
        And the service has a window period of 9 minutes
        Given a "temperature" Variation Computer
        When the service is started
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 9      | 18          | 0     | 9           | 9         |
          | 11     | 14          |       |             |           |
          | 12     |             | -4    | 9           | 12        |
          | 13     | 25          |       |             |           |
          | 15     |             | 7     | 9           | 15        |
          | 18     | 11          | -7    | 9           | 18        |
          | 21     | 1           | -13   | 12          | 21        |
          | 24     | 19          | -6    | 15          | 24        |

    Scenario: Start after more than a window retention time
        Given a TimeMath service start at 100 minutes
        And the "delta" service primary period is define to 3 minutes
        And the service has a window period of 9 minutes
        Given a "temperature" Variation Computer
        When the service is started
        Then these events occurs before system tick:
          | minute | temperature | delta | delta.Start | delta.End |
          | 100    | 10          |       |             |           |
          | 101    | 15          |       |             |           |
          | 102    |             | 5     | 100         | 102       |
          | 104    | 23          |       |             |           |
          | 105    |             | 13    | 100         | 105       |
          | 107    | 18          |       |             |           |
          | 108    |             | 8     | 100         | 108       |
          | 110    | 14          |       |             |           |
          | 111    |             | -1    | 102         | 111       |
          | 112    | 25          |       |             |           |
          | 114    |             | 2     | 105         | 114       |
          | 117    | 11          | -7    | 108         | 117       |
          | 120    | 1           | -13   | 111         | 120       |
          | 123    | 19          | -6    | 114         | 123       |