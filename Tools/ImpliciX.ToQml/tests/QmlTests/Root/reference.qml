import QtTest 1.2

TestCase {
    name: "reference"

    function test_dumb() {
        verify(1 + 1 === 2, "Reference test failed")
    }
}
