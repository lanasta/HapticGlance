library(hash)
library(crayon)
library("plotrix")

magnitudeThreshold <- 0.25 #rd, erzhen's need 0.31, pilot studies 0.35
yLim <- 1.5
summaryLim <- 2.2
factorInAbsValue <- FALSE
averageMagnitude <- 0

minAcc <- 100.0
maxAcc <- 0.0
minMag <- 100.0
maxMag <- 0.0

calculateDirectionBasedErrorAngle <- function (angle) {
  if (direction == "ne" || direction == "se" || direction == "nw" || direction == "sw") {
    angle <- 45 - abs(angle)
  }else if (direction == "n") {
    angle <- 90 - abs(angle)
  } else if (direction == "s") {
    angle <- abs(angle) - 90
  }
  cat("\n",direction, angle)
  return (angle)
}

analyzeThresholdPoints <- function(mydata) {
  magnitude <- mydata$Magnitude
  firstPassPt <- mydata[min(which(magnitude >= magnitudeThreshold)),]
  secondPassPt <- mydata[max(which(magnitude >= magnitudeThreshold)),]
  peakPt <- mydata[which(magnitude == max(magnitude)),]
  firstPassAngle <- atan(firstPassPt$Fy/firstPassPt$Fx) * 180 / pi
  secondPassAngle <- atan(secondPassPt$Fy/secondPassPt$Fx) * 180 / pi
  peakPtAngle <- atan(peakPt$Fy/peakPt$Fx) * 180 / pi
  firstPassErrorAngle <- calculateDirectionBasedErrorAngle(firstPassAngle)
  secondPassErrorAngle <- calculateDirectionBasedErrorAngle(secondPassAngle)
  peakPtErrorAngle <- calculateDirectionBasedErrorAngle(peakPtAngle)
  newlist <- list(firstPassErrorAngle, secondPassErrorAngle, peakPtErrorAngle, firstPassAngle, secondPassAngle, peakPtAngle)
  return (newlist)
}

plotForceVectorLine <- function(Fx, Fy, colorRandom) {
  par(new = TRUE)
  plot(Fx, Fy, ylim=range(c(-yLim,yLim)),  xlim=c(-yLim,yLim), type="l", axes = FALSE, xlab = "", ylab = "", col = colorRandom)
}

clipBasedOnDirection <- function(direction){
  x1 <- -1
  x2 <- 1
  y1 <- -1
  y2 <- 1
  if (direction == "n") {
    x1<- -1
    x2 <- 1
    y1 <- 0
  } else if (direction == "s") {
    x1<- -1
    x2 <- 1
    y2 <- 0
  } else if (direction == "e") {
    x1 <- 0
  } else if (direction == "w") {
    x2 <- 0
  } 
  clip(x1, x2, y1, y2)
}

analyzeRegressionLine <- function(Fx, Fy, color) {
  #axis lines
  regressionErrorAngle <- 0

  if (direction == "n" || direction == "s") {
    abline(v=0,  col="blue")
  } else {
    #regression and expected line
    abline(0, 0,  col="blue")
  }

  clipBasedOnDirection(direction)
  fit.lm <-lm(Fy ~ Fx)
  abline(lm(Fy~Fx), col=color, lwd=2)
  slope <- coef(fit.lm)[2]
  regressionAngle <-  atan(slope) * 180 / pi
  regressionErrorAngle <- calculateDirectionBasedErrorAngle(regressionAngle)
  cat("\nRegression angle:", atan(slope) * 180 / pi)
  newlist <- list(regressionErrorAngle, as.numeric(atan(slope) * 180 / pi))
  return (newlist)
}


plotForceVector <- function(direction){
  plot(0, 0, main=paste("2D Force Vector", toupper(direction)), xlab="Fx", ylab="Fy", xlim=c(-yLim,yLim), ylim=c(-yLim, yLim))
  abline(0, 0, col="black")
  abline(v=0, col="black")
  fileCount <- 0
  for (filename in myFiles) {
    fileCount <- fileCount + 1
    ds <- read.csv(filename, skip = 2)
    name <- basename(filename)
    Fx <- ds$Fx
    Fy <- ds$Fy
    mydata <- ds[ds$Magnitude > 0.10,]
    magnitude <- mydata$Magnitude
    colorRandom <- getRandomColor(FALSE)
    plotForceVectorLine(Fx, Fy, colorRandom)
    regressionAngles <- analyzeRegressionLine(Fx,Fy,colorRandom)
    regressionErrorList <- c(regressionErrorList, calcByAbsMode(as.numeric(regressionAngles[1]), 1))
    regAngles <- c(regAngles, as.numeric(regressionAngles[2]))
    h[direction] <- regressionErrorList
  }
  print(regAngles)
  cat("\nAverage regression error:", mean(regressionErrorList), "\n")
  regAnglesDir[direction] <- mean(regAngles)
}

plotMagnitude <- function(direction) {
  first <- TRUE
  fileCount <- 0
  sumMaxMag <- 0
  for (filename in myFiles) {
    ds <- read.csv(filename, skip=2)
    mydata <- ds[ds$Magnitude > 0.14,]
    
    fileCount <- fileCount + 1
    timestamp <- mydata$Timestamp
    magnitude <- mydata$Magnitude
    sumMaxMag <- sumMaxMag + max(magnitude)
  }
  magAverage <- sumMaxMag/fileCount
  cat("\nAverage Max magnitude: ", magAverage)
}

assessAccuracy <- function(direction) {
  fileCount <- 0
  correctCount <- 0
  sumAvgIncorrectAnswer <- 0
  incorrectAnswerFileCount <- 0
  for (filename in myFiles) {
    accData <- read.csv(filename, header=FALSE, sep = ",", skip=1, nrows=1)
    
    fileCount <- fileCount + 1
    
    if (accData[1] == "True")  {
      correctCount <- correctCount + 1
    } else {
      sumAvgIncorrectAnswer <- sumAvgIncorrectAnswer + accData[1,2]
      incorrectAnswerFileCount <- incorrectAnswerFileCount + 1
    }
      
    # correctness <- accData$Correct
    # userAnswer <- accData$`User Answer`
    # correctAnswer <- accData$`Correct Answer`
    # print("User answer: " + correctness)
  }
  #cat("\ncorrect: ", correctCount, "/", fileCount)
  avgAnswer <- sumAvgIncorrectAnswer/incorrectAnswerFileCount
 # cat("\naverage answer: ", avgAnswer)
  accuracy <- correctCount/fileCount * 100
  cat("\nAccuracy: ", accuracy, "%")
}

getAdjustedAngle <- function(angle, direction){
  # if (direction == "se") {
  #   return (angle + 90)
  # } else if (direction == "e") {
  #   return(angle)
  # }
  return(angle)
}

getRandomColor <- function(opaque){
  opacity <- 0.7
  if (opaque == TRUE) {
    opacity <- 1
  }
  return(rgb(runif(1, 0.5, 1.0), runif(1, 0.2, 1.0), runif(1, 0.3, 1.0), opacity))
}

drawGuideLines <- function(direction) {
  abline(v=0, col="black")
  abline(h=0, col="black")
  if (direction == "n" || direction == "s") {
    abline(v=0, col="blue", lw=1)
  } else if (direction == "e" || direction == "w") {
    abline(h=0, col="blue", lw=1)
  } else if (direction == "ne" || direction == "sw") {
    abline(coef = c(0,1), col="blue", lw=1)
  } else {
    abline(coef = c(0,-1), col="blue", lw=1)
  }
}

calcByAbsMode <- function(errorAngle, pos) {
  angle <- errorAngle
  # if (pos == 1 && errorAngle >= 0 || pos == 0 && errorAngle < 0){
  #   angle <- errorAngle
  # }
  if (factorInAbsValue) {
    return(abs(angle))
  } else {
    return(angle)
  }
}

directions <- list("n", "e", "s", "w")
par(mfcol=c(4,6))
par(pty="s")
par(mar=c(2,1,2,1))
bpYLim <- 40 #box plot y-limit

pattern <- paste("irb*/HapticMarkExp*Block-*MarkCount-2*.csv", sep="")
#calculate average error
myFiles <- Sys.glob(pattern)

plotMagnitude(direction)
assessAccuracy(direction)





