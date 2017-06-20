
Task("Watch").Does(() => {
    // watchman-make -p '**/*.fs' --make='./build.sh --target' -t Build
    StartProcess("watchman-make", new ProcessSettings {
        Arguments = "-p '**/*.fs' --make='./build.sh --target' -t Build"
    });
});