library(hash)
library(crayon)
library("plotrix")

accData <- read.csv("JNDExpAnalysisSummary.csv", sep = ",")
names(accData)
par(mar=c(7,5,2,1))
par(cex.lab=1.2)
boxplot(accData,
        main = "JND",
        ylab = "N",
        col = "lightblue",
        border = "forestgreen",
        horizontal = FALSE,
        las=2
)


points(mean(accData$Average), col="blue", pch=18)

text(locator(1),"mean = 0.285", cex=0.8, pos=3,col="black") 
text(locator(1),"median = 0.2625", cex=0.8, pos=3,col="black") 
text(locator(1),"min = 0.1875", cex=0.8, pos=3,col="black") 
text(locator(1),"max = 0.5375", cex=0.8, pos=3,col="black") 




