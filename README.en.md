# Official Site

[https://cmwtat.cloudmoe.com]

# CloudMoe Windows 10+ Activation Toolkit Digital Edition

This toolkit can activate your Windows 10 and Windows 11 use digital license.  

A Windows 10 and Windows 11 digital license activation tool written in CSharp.  

![UI Screenshot][UI_image]

# Usage

## Getting started

### Use Auto Mode to activate `Windows 10` or `Windows 11`

1. Download release `.exe` file.

2. Run it.

3. Click `Activate` button.

4. Enjoy it :)

## Advanced

### Switch between different editions of `Windows`

* Note: Currently, it is known that `Windows` `Professional`, `ProfessionalWorkstation`, `Education`, `ProfessionalEducation`, and `Enterprise` editions can be switched between each other (except for N and LTSB editions), and these editions cannot be directly converted to `Home (Core)` edition with one click. If you need to switch, please use the `Upgrade to full version of Windows` function added in version 2.6 (this function only appears on the core edition of the operating system) or use the `Change product key` function in `Windows Settings` to upgrade.  
You need to activate the `Enterprise` edition before activating the `IoTEnterprise` edition.

##### Auto Mode

1. Run it.

2. Select `Auto Mode` 。

3. Select the edition you want to upgrade to from the drop-down list.

4. Press the `Convert versions` conversion button.

5. Done.

##### Manual Mode

* Note: This method does not work for activating some versions, such as `Professional Education (ProfessionalEducation)`, even if you enter the corresponding OEM retail key.

1. Run it.

2. Select `Manual Mode`.

3. Enter the OEM retail key corresponding to the version you want to upgrade to in the input box (you do not need the activation key on the product packaging, but the key assigned by Microsoft official, such as the professional version corresponding key is `VK7JG-NPHTM-C97JM-9MPGT-3V66T`).

4. Press the `Convert versions` conversion button.

5. Done.

### Activate a `Windows` edition that is not in the list using `Manual Mode`

1. Run it.

2. Select `Manual Mode`.

3. Enter the OEM retail key corresponding to the version you want to upgrade to in the input box (you do not need the activation key on the product packaging, but the key assigned by Microsoft official, such as the professional version corresponding key is `VK7JG-NPHTM-C97JM-9MPGT-3V66T`).

4. Press the `Activate` conversion button.

5. Done.

## Startup Parameters

```
-?  --help            Pop up the startup parameter help dialog after startup.
-a  --auto            Automatically activate the system after startup.
-e  --expact          Allow the use of experimental schemes when automatically activating the system. (Need to be used with -a or --auto)
-h  --hide            Start in hidden mode, the activation progress is displayed in the form of notifications. (Need to be used with -a or --auto)
```

# License

[GPL-2.0](./LICENSE)

# Contributors

[https://github.com/TGSAN/CMWTAT_Digital_Edition/graphs/contributors]

[UI_image]:./images/UI.jpg
[https://cmwtat.cloudmoe.com]:https://cmwtat.cloudmoe.com
[https://github.com/TGSAN/CMWTAT_Digital_Edition/graphs/contributors]:https://github.com/TGSAN/CMWTAT_Digital_Edition/graphs/contributors
