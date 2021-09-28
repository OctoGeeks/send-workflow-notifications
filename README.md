# Send Workflow Notifications

The out of the box option for setting up notifications for GitHub Actions runs leaves something to be desired.

I'm sure that the Actions team will make improvements there in due time, in the meantime this solution can give you alot more flexibility for setting up email notifictions around Actions.

# How does it work?

First you need to commit a YML file to your repo to configure who gets notified and when. This can be stored anywhere in the repo, but we suggest .github/workflow-notifications.yml

```
workflows:
- workflow: CI Build
  notifications:
  - email: homer.simpson@springfield.com
    filter: [ failed ]

- workflow: Production Deploy
  notifications:
  - email: mona@github.com
    filter: [ requested, succeeded, failed ]
  - email: rickandmorty@rickiverse.com
    filter: [ failed ]
```

There is a section for each Workflow you want to configure notifications for, then a list of emails (these could be email groups/aliases also) that want to receive notifications. The 3 options for filter are requested, succeeded and failed depending on which events for that particular workflow you want to be notified on.

Then you need to create a new workflow file that runs this custom action and feeds in the config file above. Here is an example:

```
name: Workflow Notifications

on:
  workflow_run:
    workflows: [ "CI Build", "Production Deploy" ]

jobs:
  notifications:

    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v2
    - uses: octogeeks/send-workflow-notifications@v1
      with:
        smtp-server: smtp.gmail.com
        smtp-user: your.email.address@gmail.com
        smtp-password: ${{ secrets.SMTP_PASSWORD }}
        configuration: ./.github/workflow-notifications.yml
```

In the "on" section, you need it to trigger on the workflow_run event, and you need to list all the workflows which you want to enable notifications on (there is no ability to use a wildcard AFAIK).

Then checkout the repo (so you get the notifications config file) and call this action passing in the details of an SMTP server that can be used to send the emails.

If you're doing this for your company, you likely already have a company email server that you can use (though you might need to talk to your email server admins to get som credentials).

Alternatively you can use a gmail account and the SMTP capabilities that gmail provides (there are some limits to how many emails you can send via SMTP per day). To use gmail use smtp.gmail.com as the smtp-server, and your gmail username and password (put the password into a github secret).  You may need to configure the gmail account to allow "less secure apps":

![image](https://user-images.githubusercontent.com/1508559/135171282-1b3eb4bc-0d82-41c5-a1ce-b38375aba698.png)
