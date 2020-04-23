library(hash)
library(crayon)

magnitudeThreshold <- 0.4
factorInAbsValue <- TRUE

calculateDirectionBasedErrorAngle <- function (angle) {
  cat("\n",direction, angle)
  if (direction == "se" || direction == "nw" || direction == "sw" || direction == "ne") {
    angle <- abs(angle) - 45
  } else if (direction == "n" || direction == "s") {
    angle <- abs(abs(angle) - 90)
  }
  cat("\n",direction, angle)
  return (angle)
}

analyzeThresholdPoints <- function(mydata) {
  magnitude <- mydata$Magnitude
  firstPassPt <- mydata[min(which(magnitude >= magnitudeThreshold)),]
  secondPassPt <- mydata[max(which(magnitude >= magnitudeThreshold)),]
  peakPt <- mydata[which(magnitude == max(magnitude)),]
  firstPassErrorAngle <- calculateDirectionBasedErrorAngle(atan(firstPassPt$Fy/firstPassPt$Fx) * 180 / pi)
  secondPassErrorAngle <- calculateDirectionBasedErrorAngle(atan(secondPassPt$Fy/secondPassPt$Fx) * 180 / pi)
  peakPtErrorAngle <- calculateDirectionBasedErrorAngle(atan(peakPt$Fy/peakPt$Fx) * 180 / pi)
  newlist <- list(firstPassErrorAngle, secondPassErrorAngle, peakPtErrorAngle)
  return (newlist)
}

plotForceVectorLine <- function(Fx, Fy, colorRandom) {
  par(new = TRUE)
  plot(Fx, Fy, ylim=range(c(-1.5,1.5)),  xlim=c(-1.5,1.5), type="l", axes = FALSE, xlab = "", ylab = "", col = colorRandom)
}

clipBasedOnDirection <- function(direction){
  x1 <- -1
  x2 <- 1
  y1 <- -1
  y2 <- 1
  if (direction == "n") {
    y1 <- 0
  } else if (direction == "s") {
    y2 <- 0
  } else if (direction == "e") {
    x1 <- 0
  } else if (direction == "w") {
    x2 <- 0
  } else if (direction == "ne") {
    y1 <- 0
    x1 <- 0
  } else if (direction == "se") {
    y2 <- 0
    x1 <- 0
  } else if (direction == "sw") {
    y2 <- 0
    x2 <- 0
  } else if (direction == "nw") {
    y1 <- 0
    x2 <- 0
  }
  clip(x1, x2, y1, y2)
  
}

analyzeRegressionLine <- function(Fx, Fy, color) {
  #axis lines
  regressionErrorAngle <- 0
  if (direction == "se" || direction == "nw"){
    abline(0,-1, col="blue")
  } else if (direction == "sw" || direction == "ne"){
    abline(0,1, col="blue")
  } else {
    if (direction == "n" || direction == "s") {
      abline(v=0,  col="blue")
    } else {
      #regression and expected line
      abline(0, 0,  col="blue")
    }
  }
  clipBasedOnDirection(direction)
  fit.lm <-lm(Fy ~ Fx)
  abline(lm(Fy~Fx), col=color, lwd=2)
  slope <- coef(fit.lm)[2]
  regressionErrorAngle <- calculateDirectionBasedErrorAngle(atan(slope) * 180 / pi)
  #cat("Error angle from regression line:", regressionErrorAngle)
  return(regressionErrorAngle)
}


plotForceVector <- function(direction){
  plot(0, 0, main=paste("2D Force Vector", toupper(direction)), xlab="Fx", ylab="Fy", xlim=c(-1.5,1.5), ylim=c(-1.5, 1.5))
  abline(0, 0, col="black")
  abline(v=0, col="black")
  fileCount <- 0
  for (filename in myFiles) {
    fileCount <- fileCount + 1
    ds <- read.csv(filename)
    name <- basename(filename)
    Fx <- ds$Fx
    Fy <- ds$Fy
    mydata <- ds[ds$Magnitude > 0.006,]
    magnitude <- mydata$Magnitude
    colorRandom <- getRandomColor()
    plotForceVectorLine(Fx, Fy, colorRandom)
    regressionPosErrorList <- c(regressionPosErrorList, calcByAbsMode(analyzeRegressionLine(Fx,Fy, colorRandom), 1))
    regressionNegErrorList <- c(regressionNegErrorList, calcByAbsMode(analyzeRegressionLine(Fx,Fy, colorRandom), 0))
    h[direction] <- regressionPosErrorList
    h7[direction] <- regressionNegErrorList
  }
  cat("\nAverage regression (+) error:", mean(regressionPosErrorList), "\n")
  cat("Average regression (-) error:", mean(regressionNegErrorList), "\n")
}

plotMagnitude <- function(direction) {
  first <- TRUE
  fileCount <- 0
  for (filename in myFiles) {
    ds <- read.csv(filename)
    mydata <- ds[ds$Magnitude > 0.006,]
    fileCount <- fileCount + 1
    timestamp <- mydata$Timestamp
    magnitude <- mydata$Magnitude
    
    #get point where graph crosses threshold for the first and last time, and peak point
    errorAnglesList <- analyzeThresholdPoints(mydata)
    firstThresholdPassPosErrorList <- c(firstThresholdPassPosErrorList, calcByAbsMode(as.numeric(errorAnglesList[1]), 1))
    firstThresholdPassNegErrorList <- c(firstThresholdPassNegErrorList, calcByAbsMode(as.numeric(errorAnglesList[1]), 0))
    secondThresholdPassPosErrorList <- c(secondThresholdPassPosErrorList, calcByAbsMode(as.numeric(errorAnglesList[2]), 1))
    secondThresholdPassNegErrorList <- c(secondThresholdPassNegErrorList, calcByAbsMode(as.numeric(errorAnglesList[2]), 0))
    peakPointPosErrorList <- c(peakPointPosErrorList, calcByAbsMode(as.numeric(errorAnglesList[3]), 1))
    peakPointNegErrorList <- c(peakPointNegErrorList, calcByAbsMode(as.numeric(errorAnglesList[3]), 0))
    
    h1[direction] <- firstThresholdPassPosErrorList
    h2[direction] <- secondThresholdPassPosErrorList
    h3[direction] <- peakPointPosErrorList
    
    h4[direction] <- firstThresholdPassNegErrorList
    h5[direction] <- secondThresholdPassNegErrorList
    h6[direction] <- peakPointNegErrorList
    
    colorRandom <- getRandomColor()
    if (first) {
      plot(timestamp, magnitude, main=paste("Magnitude", toupper(direction)), ylim=c(0, 1.4), xlab="ms", ylab="N", type="l",  col=colorRandom)
      abline(magnitudeThreshold, 0, col="brown", lwd=2)
      first <- FALSE
    } else {
      par(new = TRUE)
      plot(timestamp, magnitude, ylim=c(0, 1.5), xlab="", ylab="", xaxt='n', type="l", axes= FALSE, col=colorRandom )
    }
    if (fileCount == length(myFiles)) {
      cat("Average threshold first pass (+) error:", mean(firstThresholdPassPosErrorList), "\n")
      cat("Average threshold first pass (-) error:", mean(firstThresholdPassNegErrorList), "\n")
      cat("Average threshold second pass (+) error:", mean(secondThresholdPassPosErrorList), "\n")
      cat("Average threshold second pass (-) error:", mean(secondThresholdPassNegErrorList), "\n")
      cat("Average peak point (+) error:", mean(peakPointPosErrorList), "\n")
      cat("Average peak point (-) error:", mean(peakPointNegErrorList), "\n")
    }
  }
}

getRandomColor <- function(){
  return(rgb(runif(1, 0, 1.0), runif(1, 0, 1.0), runif(1, 0, 1.0), 0.4))
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
  angle <- 0
  if (pos == 1 && errorAngle >= 0 || pos == 0 && errorAngle < 0){
    angle <- errorAngle
  }
  if (factorInAbsValue) {
      return(abs(angle))
  } else {
    return(angle)
  }
}

directions <- list("n", "ne", "e", "se", "s", "sw", "w", "nw")
par(mfcol=c(4,6))
par(pty="s")
par(mar=c(2,1,2,1))
h <- hash() 
h1 <- hash() 
h2 <- hash() 
h6 <- hash() 
h4 <- hash()
h5 <- hash()
h6 <- hash()
h7 <- hash()

bpYLim <- 40 #box plot y-limit

for (direction in directions) {
  cat(blue("\nDirection: ", toupper(direction)))
  pattern <- paste("DirectionDiscrimination*_" , direction , "-*.csv", sep="")
  myFiles <- Sys.glob(pattern)
  regressionPosErrorList <- c()
  regressionNegErrorList <- c()
  firstThresholdPassPosErrorList <- c()
  firstThresholdPassNegErrorList <- c()
  secondThresholdPassPosErrorList <- c()
  secondThresholdPassNegErrorList <- c()
  peakPointPosErrorList <- c()
  peakPointNegErrorList <- c()
  
  plotForceVector(direction)
  plotMagnitude(direction)
}


boxplot(h[["n"]], h[["ne"]], h[["e"]], h[["se"]], h[["s"]], h[["sw"]], h[["w"]], h[["nw"]],
        main = "Regression° (+) Error",
        names = c("N", "NE", "E", "SE", "S", "SW", "W", "NW"),
        las = 3,
        cex.axis=0.75,
        col = c("orange","red"),
        border = "brown",
        horizontal = FALSE,
        notch = FALSE,
        ylim = c(0, bpYLim)
)

boxplot(h7[["n"]], h7[["ne"]], h7[["e"]], h7[["se"]], h7[["s"]], h7[["sw"]], h7[["w"]], h7[["nw"]],
        main = "Regression°(-) Error",
        names = c("N", "NE", "E", "SE", "S", "SW", "W", "NW"),
        las = 3,
        cex.axis=0.75,
        col = c("orange","red"),
        border = "brown",
        h7orizontal = FALSE,
        notch7 = FALSE,
        ylim = c(0, bpYLim)
)

boxplot(h1[["n"]], h1[["ne"]], h1[["e"]], h1[["se"]], h1[["s"]], h1[["sw"]], h1[["w"]], h1[["nw"]],
        main = "1st T-Pass° (+) Error",
        names = c("N", "NE", "E", "SE", "S", "SW", "W", "NW"),
        las = 3,
        cex.axis=0.75,
        col = c("orange","red"),
        border = "brown",
        horizontal = FALSE,
        notch = FALSE,
        ylim = c(0, bpYLim)
)

boxplot(h4[["n"]], h4[["ne"]], h4[["e"]], h4[["se"]], h4[["s"]], h4[["sw"]], h4[["w"]], h4[["nw"]],
        main = "1st T-Pass° (-) Error",
        names = c("N", "NE", "E", "SE", "S", "SW", "W", "NW"),
        las = 3,
        cex.axis=0.75,
        col = c("orange","red"),
        border = "brown",
        horizontal = FALSE,
        notch = FALSE,
        ylim = c(0, bpYLim)
)

boxplot(h2[["n"]], h2[["ne"]], h2[["e"]], h2[["se"]], h2[["s"]], h2[["sw"]], h2[["w"]], h2[["nw"]],
        main = "2nd T-Pass° (+) Error",
        names = c("N", "NE", "E", "SE", "S", "SW", "W", "NW"),
        las = 3,
        cex.axis=0.75,
        col = c("orange","red"),
        border = "brown",
        horizontal = FALSE,
        notch = FALSE,
        ylim = c(0, bpYLim)
)

boxplot(h5[["n"]], h5[["ne"]], h5[["e"]], h5[["se"]], h5[["s"]], h5[["sw"]], h5[["w"]], h5[["nw"]],
        main = "2nd T-Pass° (-) Error",
        names = c("N", "NE", "E", "SE", "S", "SW", "W", "NW"),
        las = 3,
        cex.axis=0.75,
        col = c("orange","red"),
        border = "brown",
        horizontal = FALSE,
        notch = FALSE,
        ylim = c(0, bpYLim)
)

boxplot(h3[["n"]], h3[["ne"]], h3[["e"]], h3[["se"]], h3[["s"]], h3[["sw"]], h3[["w"]], h3[["nw"]],
        main = "Peak Point° (+) Error",
        names = c("N", "NE", "E", "SE", "S", "SW", "W", "NW"),
        las = 3,
        cex.axis=0.75,
        col = c("orange","red"),
        border = "brown",
        horizontal = FALSE,
        notch = FALSE,
        ylim = c(0, bpYLim)
)
boxplot(h6[["n"]], h6[["ne"]], h6[["e"]], h6[["se"]], h6[["s"]], h6[["sw"]], h6[["w"]], h6[["nw"]],
        main = "Peak Point° (-) Error",
        names = c("N", "NE", "E", "SE", "S", "SW", "W", "NW"),
        las = 3,
        cex.axis=0.75,
        col = c("orange","red"),
        border = "brown",
        horizontal = FALSE,
        notch = FALSE,
        ylim = c(0, bpYLim)
)


